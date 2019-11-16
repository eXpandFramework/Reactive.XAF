using System;
using System.Linq;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

namespace Xpand.TestsLib.Attributes{
    public class XpandTestAttribute:Attribute,IApplyToContext,IWrapSetUpTearDown{
        private readonly int _timeout;

        public XpandTestAttribute(int timeout=120000,int times=2){
            Times = times;
            _timeout = timeout;
        }

        void IApplyToContext.ApplyToContext(TestExecutionContext context){
            context.TestCaseTimeout=_timeout;
        }
        
        public TestCommand Wrap(TestCommand command){
            return new RetryingCommand(command, Times);
        }

        public int Times{ get; }

        private class RetryingCommand : DelegatingTestCommand{
            private readonly int _times;

            public RetryingCommand(TestCommand innerCommand, int times)
                : base(innerCommand){
                _times = times;
            }

            public override TestResult Execute(TestExecutionContext context){
                var retriesLeft = _times;

                RunTest(context);

                while (TestFailed(context) && retriesLeft > 0){
                    ClearTestResult(context);
                    RunTest(context);

                    retriesLeft--;
                }

                var performedRetries = _times - retriesLeft;

                if (performedRetries > 0){
                    context.OutWriter.WriteLine();
                    context.OutWriter.WriteLine($"Test retried {performedRetries} time/s.");
                }

                return context.CurrentResult;
            }

            private void RunTest(TestExecutionContext context){
                context.CurrentResult = innerCommand.Execute(context);
            }

            private static void ClearTestResult(TestExecutionContext context){
                context.CurrentResult = context.CurrentTest.MakeTestResult();
            }

            private static bool TestFailed(TestExecutionContext context){
                var currentResultResultState = context.CurrentResult.ResultState;
                var resultStates = UnsuccessfulResultStates;
                return resultStates.ToList().Contains(currentResultResultState);
            }

            private static ResultState[] UnsuccessfulResultStates => new[] {
                ResultState.Failure,
                ResultState.Error
            };
        }
    }
    
}