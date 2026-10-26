using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Tests.Common;
using Trail = Xpand.TestsLib.Common.Extensions;

namespace Xpand.XAF.Modules.Reactive.Tests {
    public class StartWinTestTimeoutTest : ReactiveCommonTest {
        private static readonly TimeSpan RunTimeout = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan Bound = TimeSpan.FromSeconds(20);

        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task A_chain_that_never_completes_fails_the_test_instead_of_hanging_it() {
            Trail.Trace("test: starting");
            var application = Platform.Win.NewApplication<ReactiveModule>();
            Trail.Trace("test: application created");
            DefaultReactiveModule(application);
            Trail.Trace("test: module added");

            var outcome = new TaskCompletionSource<object>();
            application.StartWinTest(_ => Observable.Never<Unit>(), RunTimeout).Subscribe(
                _ => { }, error => outcome.TrySetException(error), () => outcome.TrySetResult(null));
            Trail.Trace("test: subscribed");

            var elapsed = Stopwatch.StartNew();
            var first = await Task.WhenAny(outcome.Task, Task.Delay(Bound));
            elapsed.Stop();
            Trail.Trace($"test: completed={first == outcome.Task} after {elapsed.ElapsedMilliseconds}ms");

            if (first != outcome.Task) {
                Assert.Fail($"the run never returned within {Bound.TotalSeconds}s. trail: {Trail.RunTrail()}");
            }

            await Should.ThrowAsync<TimeoutException>(async () => await outcome.Task);
            elapsed.Elapsed.ShouldBeLessThan(Bound);
        }
    }
}
