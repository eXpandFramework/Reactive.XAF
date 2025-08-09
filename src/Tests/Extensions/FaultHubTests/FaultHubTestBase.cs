using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;

namespace Xpand.Extensions.Tests.FaultHubTests{
    public class FaultHubTestBase {
        public FaultHubTestBase() => FaultHub.Logging = true;

        protected TestObserver<Exception> BusObserver;
        protected TestObserver<Exception> PreBusObserver;

        protected class TestResource : IDisposable {
            public bool IsDisposed { get; private set; }
            public Action OnDispose { get; init; } = () => { };
            public void Dispose() {
                IsDisposed = true;
                OnDispose();
            }
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
            BusObserver = FaultHub.Bus.Test();
            PreBusObserver = FaultHub.PreBus.Test();
        }
    }
}