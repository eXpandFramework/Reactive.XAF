// using System;
// using System.Linq;
// using System.Reactive;
// using System.Reactive.Linq;
// using System.Reactive.Threading.Tasks;
// using System.Threading.Tasks;
// using NUnit.Framework;
// using Shouldly;
// using Xpand.Extensions.Reactive.ErrorHandling;
// using Xpand.Extensions.Reactive.Utility;
//
// namespace Xpand.Extensions.Tests.FaultHubTests{
//     public class TraceFaultTest : FaultHubTestBase {
//         [Test]
//         public async Task Baseline_TraceFaults_Captures_Context_From_Async_Throw() {
//             var source = Unit.Default.Defer(() => Observable.Start(() => Observable.Throw<Unit>(new InvalidOperationException("Originating Error"))).Merge());
//
//             source.TraceFaults(["context1", "context2"])
//                 .Subscribe();
//             await Task.Delay(200);
//             BusObserver.ItemCount.ShouldBe(1);
//             var publishedException = BusObserver.Items.Single();
//
//             publishedException.ShouldBeOfType<FaultHubException>();
//             var faultHubException = (FaultHubException)publishedException;
//
//             faultHubException.Context.CustomContext.ShouldContain("context1");
//             var topFrame = faultHubException.Context.DefinitionStackTrace.GetFrame(0);
//             topFrame?.GetMethod()?.DeclaringType?.Name.ShouldContain(nameof(Baseline_TraceFaults_Captures_Context_From_Async_Throw));
//         }
//         
//         [Test]
//         public async Task Solution_TraceFaults_Now_Catches_Downstream_Timeout_Exception() {
//             // This test now demonstrates the solution. The exception from Timeout() propagates
//             // upstream to the .Catch block inside our new TraceFaults() operator.
//             var sourc=this.Defer(() => Observable.Start(() => Observable.Never<Unit>().Timeout(TimeSpan.FromMilliseconds(50))).Merge());
//
//             sourc.TraceFaults(["ContextWithTimeout"])
//                 .Subscribe();
//
//             await Task.Delay(200);
//
//             BusObserver.ItemCount.ShouldBe(1);
//             var publishedException = BusObserver.Items.Single();
//
//             // The assertion now passes. TraceFaults successfully caught and wrapped
//             // the TimeoutException, solving the original problem.
//             publishedException.ShouldBeOfType<FaultHubException>();
//             publishedException.InnerException.ShouldBeOfType<TimeoutException>();
//         }        
// }
// }