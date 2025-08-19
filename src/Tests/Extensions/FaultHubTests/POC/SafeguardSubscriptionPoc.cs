using System;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Utility;
using System.Reactive;

namespace Xpand.Extensions.Tests.FaultHubTests.POC {
    [TestFixture]
    public class SafeguardSubscriptionPoc : FaultHubTestBase {
        // MODIFICATION: Added a local, self-contained implementation of the helper
        // to resolve the compilation error and make the POC runnable.
        

        // This is the incomplete pattern, missing the final safeguard.
        private IObservable<T> ApplyIncompleteResiliencePattern<T>(IObservable<T> source,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) {
            return source
                .PushStackFrame(memberName, filePath, lineNumber)
                .SuppressAndPublishOnFault(null, memberName, filePath, lineNumber);
        }
        
        // This is the complete, correct pattern with the safeguard.
        private IObservable<T> ApplyCompleteResiliencePattern<T>(IObservable<T> source,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) {
            return source
                .PushStackFrame(memberName, filePath, lineNumber)
                .SuppressAndPublishOnFault(null, memberName, filePath, lineNumber)
                .SafeguardSubscription((ex, _) => ex.ExceptionToPublish(new object[]{}.NewFaultContext(FaultHub.LogicalStackContext.Value, memberName)).Publish());
        }

        [Test]
        public void Incomplete_Pattern_Fails_To_Handle_Exception_From_Disposal() {
            
            // Create a resource that will throw an exception when Dispose() is called.
            var resource = new TestResource { OnDispose = () => throw new InvalidOperationException("Dispose Failed") };
            var sourceWithFailingDispose = Observable.Using(() => resource, _ => Observable.Return(42));
            
            // ACT & ASSERT
            // We expect this to throw because the resilience pattern is missing the safeguard.
            Should.Throw<InvalidOperationException>(() => {
                var stream = ApplyIncompleteResiliencePattern(sourceWithFailingDispose);
                // Subscribing will trigger the stream, which completes, then attempts disposal, which throws.
                stream.Subscribe();
            });
            
            // The bus should be empty because the exception was unhandled and never published.
            BusEvents.Count.ShouldBe(0);
        }

        [Test]
        public void Complete_Pattern_With_SafeguardSubscription_Handles_Exception_From_Disposal() {
            
            var resource = new TestResource { OnDispose = () => throw new InvalidOperationException("Dispose Failed") };
            var sourceWithFailingDispose = Observable.Using(() => resource, _ => Observable.Return(42));
            
            
            // We apply the complete pattern, which includes the safeguard.
            var stream = ApplyCompleteResiliencePattern(sourceWithFailingDispose);
            
            // We do not expect this to throw.
            Should.NotThrow(() => stream.Subscribe());

            // ASSERT
            // The bus should now contain the exception because SafeguardSubscription caught and published it.
            BusEvents.Count.ShouldBe(1);
            BusEvents[0].ShouldBeOfType<FaultHubException>()
                .InnerException.ShouldBeOfType<InvalidOperationException>()
                .Message.ShouldBe("Dispose Failed");
        }
    }

    static class SafeguardSubscriptionPocEx {
        public static IObservable<T> SuppressAndPublishOnFault<T>(this IObservable<T> source,
            object[] context, string memberName, string filePath, int lineNumber)
            => source.Materialize()
                .Select(notification => {
                    if (notification.Kind != NotificationKind.OnError) return notification;
                    var faultContext = context.NewFaultContext(FaultHub.LogicalStackContext.Value,memberName, filePath, lineNumber);
                    notification.Exception.ExceptionToPublish(faultContext).Publish();
                    return Notification.CreateOnCompleted<T>();
                })
                .Dematerialize();

    }
}