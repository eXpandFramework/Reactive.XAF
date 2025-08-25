using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using NUnit.Framework;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;

namespace Xpand.Extensions.Tests.FaultHubTests{
    public class FaultHubTestBase {
        protected List<Exception> BusEvents;
        private IDisposable _busSubscription;
        public FaultHubTestBase() => FaultHub.Logging = true;

        protected class TestResource : IDisposable {
            public bool IsDisposed { get; private set; }
            public Action OnDispose { get; init; } = () => { };
            public void Dispose() {
                IsDisposed = true;
                OnDispose();
            }
        }

        [TearDown]
        public void TearDown() {
            _busSubscription?.Dispose();
        }
        
        protected static IEnumerable<TestCaseData> RetrySelectors() {
            yield return new TestCaseData(RetrySelector).SetName("Retry");
            yield return new TestCaseData(RetrySelectorWithBackoff).SetName("RetrySelector");
        }

        private static Func<IObservable<Unit>,IObservable<Unit>> RetrySelector=>source => source.Retry(3);
        private static Func<IObservable<Unit>,IObservable<Unit>> RetrySelectorWithBackoff=>source => source.RetryWithBackoff(3, strategy:_ => 50.Milliseconds());
        [SetUp]
        public void Setup(){
            FaultHub.Seen.Clear();  
            BusEvents = new List<Exception>();
            _busSubscription = FaultHub.Bus.Subscribe(BusEvents.Add);
        }
        
    }
}