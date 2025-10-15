using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Tests.FaultHubTests.ProofOfConcepts {
    [TestFixture]
    public class SafeguardSubscriptionPoc : FaultHubTestBase {
        
        
        

        
        private IObservable<T> ApplyIncompleteResiliencePattern<T>(IObservable<T> source,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => source
                .PushStackFrame(memberName, filePath, lineNumber)
                .SuppressAndPublishOnFault(null, memberName, filePath, lineNumber);


        private IObservable<T> ApplyCompleteResiliencePattern<T>(IObservable<T> source,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) {
            return source
                .PushStackFrame(memberName, filePath, lineNumber)
                .SuppressAndPublishOnFault(null, memberName, filePath, lineNumber)
                .SafeguardSubscription((ex, _) => ex.ExceptionToPublish(FaultHub.LogicalStackContext.Value.NewFaultContext([],null,memberName)).Publish());
        }

        [Test]
        public void Incomplete_Pattern_Fails_To_Handle_Exception_From_Disposal() {
            
            
            var resource = new TestResource { OnDispose = () => throw new InvalidOperationException("Dispose Failed") };
            var sourceWithFailingDispose = Observable.Using(() => resource, _ => Observable.Return(42));
            
            
            
            Should.Throw<InvalidOperationException>(() => {
                var stream = ApplyIncompleteResiliencePattern(sourceWithFailingDispose);
                
                stream.Subscribe();
            });
            
            
            BusEvents.Count.ShouldBe(0);
        }

        [Test]
        public void Complete_Pattern_With_SafeguardSubscription_Handles_Exception_From_Disposal() {
            
            var resource = new TestResource { OnDispose = () => throw new InvalidOperationException("Dispose Failed") };
            var sourceWithFailingDispose = Observable.Using(() => resource, _ => Observable.Return(42));
            
            
            
            var stream = ApplyCompleteResiliencePattern(sourceWithFailingDispose);
            
            
            Should.NotThrow(() => stream.Subscribe());

            
            
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
                    var faultContext = FaultHub.LogicalStackContext.Value.NewFaultContext(context,null,memberName, filePath);
                    notification.Exception.ExceptionToPublish(faultContext).Publish();
                    return Notification.CreateOnCompleted<T>();
                })
                .Dematerialize();

    }
}