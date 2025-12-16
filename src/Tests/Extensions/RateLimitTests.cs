using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Transform;
using Xpand.TestsLib;

namespace Xpand.Extensions.Tests {
    public class RateLimitTests : BaseTest {
        [Test]
        public async Task RateLimit_Should_Throttle_Emissions() {
            const int count = 5;
            const double ratePerSecond = 10;
            var expectedDuration = TimeSpan.FromSeconds((count - 1) / ratePerSecond);

            var stopwatch = Stopwatch.StartNew();

            await Observable.Range(1, count)
                .RateLimit(ratePerSecond, Guid.NewGuid().ToString())
                .Buffer(count);

            stopwatch.Stop();

            stopwatch.Elapsed.ShouldBeGreaterThanOrEqualTo(expectedDuration);
            stopwatch.Elapsed.TotalSeconds.ShouldBeLessThan(expectedDuration.TotalSeconds + 0.5);
        }

        [Test]
        public async Task RateLimit_Should_Isolate_Different_Groups() {
            const double ratePerSecond = 1;
            var groupA = Guid.NewGuid().ToString();
            var groupB = Guid.NewGuid().ToString();

            var stopwatch = Stopwatch.StartNew();

            var taskA = Observable.Return(1).Repeat(2).RateLimit(ratePerSecond, groupA).ToTask();
            var taskB = Observable.Return(1).Repeat(2).RateLimit(ratePerSecond, groupB).ToTask();

            await Task.WhenAll(taskA, taskB);

            stopwatch.Stop();

            stopwatch.Elapsed.TotalSeconds.ShouldBeInRange(0.9, 1.9);
        }

        [Test]
        public async Task RateLimit_Should_Share_Limit_Across_Subscriptions_Same_Group() {
            const double ratePerSecond = 2;
            var group = Guid.NewGuid().ToString();

            var stopwatch = Stopwatch.StartNew();

            await Observable.Return(1).RateLimit(ratePerSecond, group);

            await Observable.Return(1).RateLimit(ratePerSecond, group);

            stopwatch.Stop();

            stopwatch.Elapsed.TotalMilliseconds.ShouldBeGreaterThan(450);
        }

        [Test]
        public async Task RateLimit_Should_PassThrough_When_Rate_Is_Zero() {
            var stopwatch = Stopwatch.StartNew();

            await Observable.Range(1, 100)
                .RateLimit(0, Guid.NewGuid().ToString())
                .Buffer(100);

            stopwatch.Stop();

            stopwatch.Elapsed.TotalMilliseconds.ShouldBeLessThan(100);
        }
    }
}