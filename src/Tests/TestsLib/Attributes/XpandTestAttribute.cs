using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using Polly.Timeout;

namespace Xpand.TestsLib.Attributes{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class XpandTestAttribute : Attribute{
        private readonly int _tryCount;
        private readonly int _timeout;

        public XpandTestAttribute(int timeout = 120000, int tryCount = 2){
            _timeout = timeout;
            _tryCount = tryCount;
        }

//        public TestCommand Wrap(TestCommand command){
//            return new RetryCommand(command, _tryCount, _timeout);
//        }


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
                        Polly.Policy.Timeout(TimeSpan.FromMilliseconds(_timeout), TimeoutStrategy.Pessimistic)
                            .Execute(() => context.CurrentResult = innerCommand.Execute(context));
                    }
                    catch (Exception ex){
                        if (context.CurrentResult == null) context.CurrentResult = context.CurrentTest.MakeTestResult();
                        context.CurrentResult.RecordException(ex);
                    }

                    if (count > 0){
                        context.CurrentResult = context.CurrentTest.MakeTestResult();
                        context.CurrentRepeatCount++;
                    }
                }

                return context.CurrentResult;
            }
        }
    }
}