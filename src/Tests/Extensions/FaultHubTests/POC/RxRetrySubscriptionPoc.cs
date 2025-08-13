using System;
using System.Reactive.Linq;
using NUnit.Framework;
using Shouldly;

namespace Xpand.Extensions.Tests.FaultHubTests.POC;
[TestFixture]
public class RxRetrySubscriptionPoc {
    [Test]
    public void Downstream_Observer_Is_Subscribed_To_Only_Once_Despite_Upstream_Retries() {
        var sourceSubscriptions = 0;
        var meterSubscriptions = 0;

        
        var source = Observable.Defer(() => {
            sourceSubscriptions++;
            Console.WriteLine($"Source was subscribed to. Count: {sourceSubscriptions}");
            return Observable.Throw<int>(new InvalidOperationException());
        });

        
        var streamWithMeter = Observable.Defer(() => {
            meterSubscriptions++;
            Console.WriteLine($"Meter was subscribed to. Count: {meterSubscriptions}");
            // 3. The Composition: The meter is downstream from Retry.
            return source.Retry(3);
        });

        
        streamWithMeter.Subscribe(_ => {}, _ => {});
        
        
        sourceSubscriptions.ShouldBe(3);
        
        meterSubscriptions.ShouldBe(1);
    }
}