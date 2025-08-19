using System;
using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.TestsLib.Common;

namespace Xpand.Extensions.Tests {
    public class RetryWithBackoffTests:CommonTest {
        [TestCase(1,2)]
        [TestCase(2,4)]
        [TestCase(3,8)]
        [TestCase(4,16)]
        [TestCase(5,32)]
        [TestCase(6,64)]
        [TestCase(7,128)]
        [TestCase(8,180)]
        [TestCase(9,180)]
        public void SecondsBackoffStrategy_Strategy(int index,int time) {
            var timeSpan = ErrorHandling.SecondsBackoffStrategy(index);
            
            timeSpan.ShouldBe(TimeSpan.FromSeconds(time));
        }
        
        [TestCase(1,400)]
        [TestCase(2,800)]
        [TestCase(3,1600)]
        [TestCase(4,3200)]
        [TestCase(5,6400)]
        [TestCase(6,12800)]
        [TestCase(7,25600)]
        [TestCase(8,36000)]
        [TestCase(9,36000)]
        public void MilliSecondsBackoffStrategy_Strategy(int index,int time) {
            var timeSpan = ErrorHandling.MilliSecondsBackoffStrategy(index);
            
            timeSpan.ShouldBe(TimeSpan.FromMilliseconds(time));
        }

        [Test]
        public void retries_indefinitely_if_no_retry_count_specified() {
            var tries = 0;
            var source = Observable.Defer(() => {
                        ++tries;
                        return Observable.Throw<Unit>(new Exception());
                    });
            source.RetryWithBackoff(scheduler: TestScheduler).Subscribe(_ => { }, _ => { });
            TestScheduler.AdvanceTimeBy(TimeSpan.FromDays(1));

            tries.ShouldBe(86401);
        }
        
        
        [TestCase(3)]
        [TestCase(1)]
        [TestCase(5)]
        [TestCase(42)]
        public void retry_count_determines_how_many_times_to_retry(int retryCount) {
            var tries = 0;
            var scheduler = new TestScheduler();
            var source = Observable.Defer(() => {
                        ++tries;
                        return Observable.Throw<Unit>(new Exception());
                    });
            source.RetryWithBackoff(retryCount, scheduler: scheduler).Subscribe(_ => { }, _ => { });
            scheduler.Start();

            tries.ShouldBe(retryCount);
        }

        [Test]
        public void default_strategy_is_exponential_backoff_to_a_maximum_of_three_minutes() {
            var tries = 0;
            var scheduler = new TestScheduler();
            var source = Observable.Defer(() => {
                        ++tries;
                        return Observable.Throw<Unit>(new Exception());
                    });
            source.RetryWithBackoff(100, scheduler: scheduler).Subscribe(_ => { }, _ => { });
            tries.ShouldBe(1);

            var @try = 1;
            for (var i = 0; i < 7; ++i) {
                var time = TimeSpan.FromSeconds(Math.Pow(2, @try)) - TimeSpan.FromMilliseconds(1);
                scheduler.AdvanceBy(time.Ticks);
                @try.ShouldBe(tries);
                scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1).Ticks);
                tries.ShouldBe(++@try);
            }

            
            for (var i = 0; i < 5; ++i) {
                var time = TimeSpan.FromMinutes(3) - TimeSpan.FromMilliseconds(1);
                scheduler.AdvanceBy(time.Ticks);
                @try.ShouldBe(tries);
                scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1).Ticks);
                tries.ShouldBe(++@try);
            }
        }
        
        [Test]
        public void strategy_determines_time_between_retries() {
            var tries = 0;
            var scheduler = new TestScheduler();
            var source = Observable.Defer(() => {
                        ++tries;
                        return Observable.Throw<Unit>(new Exception());
                    });
            source.RetryWithBackoff(100, n => TimeSpan.FromSeconds(n), scheduler: scheduler).Subscribe(_ => { }, _ => { });
            tries.ShouldBe(1);

            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(999).Ticks);
            tries.ShouldBe(1);
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1).Ticks);
            tries.ShouldBe(2);

            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1999).Ticks);
            tries.ShouldBe(2);
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1).Ticks);
            tries.ShouldBe(3);

            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(2999).Ticks);
            tries.ShouldBe(3);
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1).Ticks);
            tries.ShouldBe(4);
        }

        [Test]
        public void retry_on_error_determines_whether_a_given_exception_results_in_a_retry() {
            var tries = 0;
            var scheduler = new TestScheduler();
            var source = Observable.Defer(() => {
                        ++tries;
                        return Observable.Throw<Unit>(tries < 3 ? new InvalidOperationException() : new Exception());
            });
            source
                .RetryWithBackoff(100, retryOnError: ex => ex is InvalidOperationException, scheduler: scheduler).Subscribe(_ => { }, _ => { });
            tries.ShouldBe(1);

            scheduler.Start();
            tries.ShouldBe(3);
        }


    }
}