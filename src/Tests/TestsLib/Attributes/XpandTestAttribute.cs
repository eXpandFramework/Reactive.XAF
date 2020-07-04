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
using Xpand.Extensions.TaskExtensions;

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
                        // ManualResetEvent resetEvent = new ManualResetEvent(false);
                        TestResult Execute() => context.CurrentResult = innerCommand.Execute(context);
                        Polly.Policy.Timeout(TimeSpan.FromMilliseconds(_timeout), TimeoutStrategy.Pessimistic)
                            .Execute(() => {
                                Task.Factory.StartTask(Execute, thread => thread.SetApartmentState(GetApartmentState(context))).Wait();
                                // return;
                                // if (context.CurrentTest.Arguments.Any(o => o == (object) Platform.Web)){
                                //     ThreadPool.QueueUserWorkItem(state => {
                                //         var tuple = ((TestExecutionContext executionContext, TestCommand command)) state;
                                //         tuple.executionContext.CurrentResult = tuple.command.Execute(tuple.executionContext);
                                //         resetEvent.Set();
                                //     }, (context, innerCommand));
                                //     resetEvent.WaitOne();    
                                // }
                                // else{
                                //     Task.Factory.StartTask(Execute, thread => thread.SetApartmentState(GetApartmentState(context))).Wait();   
                                // }
                            });
                    }
                    catch (Exception ex){
                        context.CurrentResult ??= context.CurrentTest.MakeTestResult();
                        context.CurrentResult.RecordException(new Exception($"Retry {context.CurrentRepeatCount+1} of {_tryCount}",ex));
                    }

                    if (count > 0){
                        if (context.CurrentResult.ResultState!=ResultState.Success){
                            context.CurrentRepeatCount++;
                        }
                        else{
                            break;
                        }
                    }
                }

                return context.CurrentResult;
            }
            
            private static ApartmentState GetApartmentState(TestExecutionContext context){
                var apartmentAttribute = context.CurrentTest.Method.MethodInfo.Attribute<ApartmentAttribute>();
                return (ApartmentState?) apartmentAttribute?.Properties[PropertyNames.ApartmentState].OfType<object>().First() ?? Thread.CurrentThread.GetApartmentState();
            }
        }
    }
}