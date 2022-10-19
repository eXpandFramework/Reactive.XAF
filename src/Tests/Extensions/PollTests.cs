using System;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests {
    public class PollTests {
        [Test]
        public void IssuesRequestEachTimePeriod() {
            var period = TimeSpan.FromSeconds(10);
            var testScheduler = new TestScheduler();
            var request = Observable.Defer(() => Observable.Return(testScheduler.Now.Ticks));

            var observer = testScheduler.CreateObserver<long>();
            request.Poll(period, testScheduler)
                .Select(i => i.Switch(v => v, _ => -1))
                .Subscribe(observer);

            testScheduler.AdvanceTo(30.Seconds().Ticks);

            observer.Messages.AssertEqual(ReactiveTest.OnNext(10.Seconds().Ticks, 10.Seconds().Ticks),
                ReactiveTest.OnNext(20.Seconds().Ticks, 20.Seconds().Ticks),
                ReactiveTest.OnNext(30.Seconds().Ticks, 30.Seconds().Ticks)
            );
        }

        [Test]
        public void IssuesRequestEachTimePeriodAfterPreviousProcessingIsComplete() {
            var testScheduler = new TestScheduler();
            var period = TimeSpan.FromSeconds(10);

            var request = Observable.Timer(TimeSpan.FromSeconds(5), testScheduler);

            var observer = testScheduler.CreateObserver<long>();
            request.Poll(period, testScheduler)
                .Select(i => i.Switch(v => v, _ => -1))
                .Subscribe(observer);

            testScheduler.AdvanceTo(30.Seconds().Ticks);

            observer.Messages.AssertEqual(ReactiveTest.OnNext(15.Seconds().Ticks, 0L),
                ReactiveTest.OnNext(30.Seconds().Ticks, 0L)
            );
        }

        [Test]
        public void ErrorsDontStopThePolling() {
            var testScheduler = new TestScheduler();
            var period = TimeSpan.FromSeconds(10);

            var callCount = 0;
            var request = Observable.Create<string>(obs => {
                callCount++;
                return callCount == 1 ? Observable.Return("Hello").Subscribe(obs) :
                    callCount == 2 ? Observable.Throw<String>(new Exception("boom")).Subscribe(obs) :
                    Observable.Return("Back again").Subscribe(obs);
            });

            var observer = testScheduler.CreateObserver<Try<string>>();
            request.Poll(period, testScheduler)
                .Subscribe(observer);

            testScheduler.AdvanceTo(30.Seconds().Ticks);

            observer.Messages.AssertEqual(ReactiveTest.OnNext(10.Seconds().Ticks, Try<string>.Create("Hello")),
                ReactiveTest.OnNext(20.Seconds().Ticks, Try<string>.Fail(new Exception("boom"))),
                ReactiveTest.OnNext(30.Seconds().Ticks, Try<string>.Create("Back again"))
            );
        }
        
        [Test]
        public void ExampleWithTimeout() {
            var testScheduler = new TestScheduler();
            var period = TimeSpan.FromSeconds(10);

            var callCount = 0;
            var request = Observable.Create<string>(obs => {
                callCount++;
                if (callCount == 2)
                    return Observable.Never<string>().Subscribe(obs);
                return Observable.Return("response").Subscribe(obs);
            });

            var observer = testScheduler.CreateObserver<Try<string>>();
            request
                .Timeout(TimeSpan.FromSeconds(5), testScheduler)
                .Poll(period, testScheduler)
                .Subscribe(observer);

            testScheduler.AdvanceTo(40.Seconds().Ticks);

            observer.Messages.AssertEqual(ReactiveTest.OnNext(10.Seconds().Ticks, Try<string>.Create("response")),
                ReactiveTest.OnNext(25.Seconds().Ticks,
                    Try<string>.Fail(new TimeoutException("The operation has timed out."))),
                ReactiveTest.OnNext(35.Seconds().Ticks, Try<string>.Create("response"))
            );
        }
    }
}