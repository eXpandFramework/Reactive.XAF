using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.TestsLib.Common;

namespace Xpand.Extensions.Tests.FaultHubTests.ProofOfConcepts {
    [TestFixture]
    public class AsyncContextHolderPoc {
        #region Asynchronous Failing Operations

        private IObservable<Unit> Level1_FailingOperation_Async(List<string> log)
            => Observable.Timer(TimeSpan.FromMilliseconds(10))
                .SelectMany(_ => Observable.Throw<Unit>(new InvalidOperationException("DB Failure")))
                .PushStackFrameWithHolder("Level1_FailingOperation_Async", log);

        private IObservable<Unit> Level2_BusinessLogic_Async(List<string> log)
            => Level1_FailingOperation_Async(log)
                .PushStackFrameWithHolder("Level2_BusinessLogic_Async", log);

        #endregion




        private IObservable<Unit> Level1_FailingOperation_Async_Fixed(List<string> log)
            => Observable.Timer(TimeSpan.FromMilliseconds(10))
                .SelectMany(_ => Observable.Throw<Unit>(new InvalidOperationException("DB Failure")))
                .PushStackFrame_Fixed("Level1_FailingOperation_Async", log);

        private IObservable<Unit> Level2_BusinessLogic_Async_Fixed(List<string> log)
            => Level1_FailingOperation_Async_Fixed(log)
                .PushStackFrame_Fixed("Level2_BusinessLogic_Async", log);

        [Test]
        public async Task Fixed_ContextHolderPattern_Succeeds_With_Async_Error() {
            var executionLog = new List<string>();
            IReadOnlyList<LogicalStackFrame> finalStack = null;

            var stream = Level2_BusinessLogic_Async_Fixed(executionLog)
                .ChainFaultContext_Fixed(3, executionLog)
                .Catch((Exception ex) => {
                    if (ex.Data.Contains("stack")) finalStack = ex.Data["stack"] as IReadOnlyList<LogicalStackFrame>;
                    return Observable.Empty<Unit>();
                });

            await stream.Test().AwaitDoneAsync(2.Seconds());

            Console.WriteLine("--- [FIXED] Execution Log ---");
            Console.WriteLine(string.Join(Environment.NewLine, executionLog));

            
            finalStack.ShouldNotBeNull();
            finalStack.Count.ShouldBe(2);
            finalStack[1].MemberName.ShouldBe("Level2_BusinessLogic_Async");
            finalStack[0].MemberName.ShouldBe("Level1_FailingOperation_Async");
        }

        [Test]
        public async Task FaultSnapshotPattern_Fails_In_Async_Chain_Due_To_Timing() {
            var executionLog = new List<string>();
            IReadOnlyList<LogicalStackFrame> finalStack = new List<LogicalStackFrame>(); 

            var stream = Level2_BusinessLogic_Async(executionLog)
                .ChainFaultContextWithHolder(1, executionLog)
                .Catch((Exception ex) => {
                    if (ex.Data.Contains("stack")) finalStack = ex.Data["stack"] as IReadOnlyList<LogicalStackFrame>;
                    return Observable.Empty<Unit>();
                });

            await stream.Test().AwaitDoneAsync(1.Seconds());

            Console.WriteLine("--- Execution Log ---");
            Console.WriteLine(string.Join(Environment.NewLine, executionLog));
            Console.WriteLine("---------------------");
            
            (finalStack?.Count ?? 0).ShouldBe(2);
        }
    }
    }
