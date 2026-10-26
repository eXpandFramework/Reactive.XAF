using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;

namespace Xpand.XAF.Modules.Reactive.Tests {
    public class StartWinTestTimeoutTest {
        [Test]
        [Apartment(ApartmentState.STA)]
        public async Task A_chain_that_never_completes_fails_the_test_instead_of_hanging_it() {
            await using var application = Platform.Win.NewApplication<ReactiveModule>();
            var elapsed = Stopwatch.StartNew();

            await Should.ThrowAsync<TimeoutException>(async () =>
                await application.StartWinTest(_ => Observable.Never<Unit>(), 3.Seconds()));

            elapsed.Stop();
            elapsed.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(60));
        }
    }
}
