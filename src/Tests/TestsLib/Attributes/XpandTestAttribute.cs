using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fasterflect;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using Polly.Timeout;
using Xpand.Extensions.Task;

namespace Xpand.TestsLib.Attributes{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class XpandTestAttribute : Attribute,IRepeatTest{
        private readonly int _tryCount;
        private readonly int _timeout;

        public XpandTestAttribute(int timeout = 120000, int tryCount = 2){
            _timeout = timeout;
            _tryCount = tryCount;
        }

        public TestCommand Wrap(TestCommand command){
            
            return new RetryCommand(command, _tryCount, _timeout);
        }


        public class RetryCommand : DelegatingTestCommand{
            private readonly int _tryCount;
            private readonly int _timeout;


            public RetryCommand(TestCommand innerCommand, int tryCount, int timeout)
                : base(innerCommand){
                _tryCount = tryCount;
                _timeout = timeout;
            }

            public override TestResult Execute(TestExecutionContext context){
                var count = _tryCount;

                while (count-- > 0){
                    try{
                        TestResult Execute() => context.CurrentResult = innerCommand.Execute(context);
                        Polly.Policy.Timeout(TimeSpan.FromMilliseconds(_timeout), TimeoutStrategy.Pessimistic)
                            .Execute(() => Task.Factory.StartTask(Execute,GetApartmentState(context)).Result);
                    }
                    catch (Exception ex){
                        if (context.CurrentResult == null) context.CurrentResult = context.CurrentTest.MakeTestResult();
                        context.CurrentResult.RecordException(new Exception($"Retry {context.CurrentRepeatCount+1} of {_tryCount}",ex));
                    }

                    if (count > 0){
                        context.CurrentResult = context.CurrentTest.MakeTestResult();
                        context.CurrentRepeatCount++;
                    }
                }

                return context.CurrentResult;
            }

            private static ApartmentState GetApartmentState(TestExecutionContext context){
                var apartmentAttribute = context.CurrentTest.Method.MethodInfo.Attribute<ApartmentAttribute>();
                if (apartmentAttribute != null){
                    return (ApartmentState) apartmentAttribute.Properties[PropertyNames.ApartmentState].OfType<object>()
                        .First();
                }
                return Thread.CurrentThread.GetApartmentState();
            }
        }
    }
}