using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;

namespace Xpand.Extensions.Tests.FaultHubTests.POC{
    [TestFixture]
    public class SelectManyRetryPoc
    {
        [Test]
        public async Task SelectMany_With_Failing_Retried_Inner_Stream_Should_Propagate_OnError()
        {
            var attemptCount = 0;
            var source = Observable.Return(1);

            var query = source.SelectMany(_ =>
                Observable.Defer(() => {
                        attemptCount++;
                        return Observable.Throw<string>(new InvalidOperationException("Inner stream failed"));
                    })
                    .Retry(3)
            );

            var notifications = await query.Materialize().ToList();

    
            attemptCount.ShouldBe(3);
            
    
            notifications.Count.ShouldBe(1);

    
            var finalNotification = notifications.Single();
            finalNotification.Kind.ShouldBe(NotificationKind.OnError);
            finalNotification.Exception.ShouldBeOfType<InvalidOperationException>()
                .Message.ShouldBe("Inner stream failed");
        }
    }
}