using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using Microsoft.Extensions.Caching.Memory;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Channels;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tests.FaultHubTests;
using Xpand.TestsLib.Common;

namespace Xpand.Extensions.Tests {
    public class RpcChannelTests : FaultHubTestBase {
        [SetUp]
        public override void Setup() {
            var rpcChannelType = typeof(RpcChannel);
            var fieldInfo = rpcChannelType.GetField("Channels", BindingFlags.Static | BindingFlags.NonPublic);
            fieldInfo.ShouldNotBeNull();

            var channelsCache = fieldInfo.GetValue(null) as MemoryCache;
            channelsCache.ShouldNotBeNull();

            channelsCache.Compact(1.0);
            base.Setup();
        }

        [Test]
        public void Request_receives_response_from_handler() {
            var key = "test-service";
            var handler = key.HandleRequest()
                .With<Unit, string>(_ => Observable.Return("Hello, Test"));

            var requester = key.MakeRequest()
                .With<string>();

            using var handlerSub = handler.Test();
            var testObserver = requester.Test();

            testObserver.AwaitDone(1.Seconds());
            testObserver.ItemCount.ShouldBe(1);
            testObserver.Items.Single().ShouldBe("Hello, Test");
            testObserver.CompletionCount.ShouldBe(1);
        }

        [Test]
        public void Request_is_keyed_and_does_not_interfere() {
            var keyA = "ServiceA";
            var keyB = "ServiceB";

            var handlerA = keyA.HandleRequest()
                .With(Observable.Return("Response from A"));

            var requesterB = keyB.MakeRequest()
                .With<string>();

            using var handlerSub = handlerA.Test();
            using var testObserver = requesterB.Timeout(100.Milliseconds()).Test();

            testObserver.AwaitDone(200.Milliseconds());
            testObserver.ErrorCount.ShouldBe(1);
            testObserver.Errors.Single().ShouldBeOfType<TimeoutException>();
        }

        [Test]
        public void Handler_error_is_propagated_to_requester() {
            var key = "error-service";
            var testException = new InvalidOperationException("Handler failed");

            var handler = key.HandleRequest()
                .With(Observable.Defer(() => Observable.Throw<string>(testException)));

            var requester = key.MakeRequest()
                .With<string>();

            using var handlerSub = handler.Test();
            using var testObserver = requester.Test();

            testObserver.AwaitDone(1.Seconds());
            testObserver.ErrorCount.ShouldBe(1);
            testObserver.Errors.Single().ShouldBe(testException);
        }

        [Test]
        public async Task Handler_can_be_asynchronous() {
            var key = "async-service";

            var handler = key.HandleRequest()
                .With(Observable.Return("async-response").Delay(100.Milliseconds()));

            var requester = key.MakeRequest()
                .With<string>();

            using var handlerSub = handler.Test();
            var result = await requester;

            result.ShouldBe("async-response");
        }

        [Test]
        public void Multiple_requests_are_handled_correctly() {
            var key = "multi-request-service";

            var handler = key.HandleRequest()
                .With<int, int>(i => Observable.Return(i * 2));

            var request1 = key.MakeRequest().With<int, int>(10);
            var request2 = key.MakeRequest().With<int, int>(20);

            using var handlerSub = handler.Test();
            using var testObserver1 = request1.Test();
            using var testObserver2 = request2.Test();

            testObserver1.AwaitDone(1.Seconds());
            testObserver2.AwaitDone(1.Seconds());

            testObserver1.Items.Single().ShouldBe(20);
            testObserver2.Items.Single().ShouldBe(40);
        }

        [Test]
        public void HandleRequest_with_value_argument_responds_correctly() {
            var key = "argument-test-service";
            var expectedResponse = "A direct response value";

            using var handlerSubscription = key.HandleRequest(expectedResponse).Test();
            var requester = key.MakeRequest().With<string>();

            requester.Test().AwaitDone(1.Seconds()).Items.Single().ShouldBe(expectedResponse);
        }


        [Test]
        public async Task Channel_is_evicted_and_recreated_after_expiration() {
            await using var stringWriter = new StringWriter();
            var originalOutput = Console.Out;
            Console.SetOut(stringWriter);

            var key = "expiration-test";
            RpcChannel.SlidingExpiration = TimeSpan.FromMilliseconds(100);

            try {
                using (key.HandleRequest("response 1").Test()) {
                    await key.MakeRequest().With<string>();
                }

                await Task.Delay(200);

                using (key.HandleRequest("response 2").Test()) {
                    await key.MakeRequest().With<string>();
                }

                var output = stringWriter.ToString();
                var constructorCallCount = Regex.Matches(output, "RpcChannel constructor called").Count;
                constructorCallCount.ShouldBe(2, "The channel should have been evicted and recreated.");
            }
            finally {
                Console.SetOut(originalOutput);
            }
        }

        [Test]
        public void Operator_Establishes_Handler_Forwards_Tuple_And_Disposes_Handler_On_Unsubscribe() {
            var source = new Subject<(string key, IntPtr value)>();
            var testData = (key: "test-channel-1", value: new IntPtr(12345));

            var stream = source.MergeIgnored(tuple => tuple.key.HandleRequest(tuple.value));

            var observer = stream.Test();

            source.OnNext(testData);

            observer.ItemCount.ShouldBe(1);
            observer.Items[0].key.ShouldBe(testData.key);

            var receivedValue = testData.key.MakeRequest().With<IntPtr>()
                .Timeout(TimeSpan.FromSeconds(1)).FirstAsync().GetAwaiter().GetResult();
            receivedValue.ShouldBe(testData.value);

            observer.Dispose();

            Should.Throw<TimeoutException>(() => {
                testData.key.MakeRequest().With<IntPtr>()
                    .Timeout(TimeSpan.FromMilliseconds(100)).FirstAsync().GetAwaiter().GetResult();
            });
        }

        [Test]
        public async Task Nested_MakeRequest_Within_Handler_Succeeds() {
            var key1 = "key1";
            var key2 = "key2";

            using var handler2Subscription = key2.HandleRequest()
                .With<string, string>(_ => Observable.Return("Response from Key2"))
                .Subscribe();

            using var handler1Subscription = key1.HandleRequest()
                .With<string, string>(requestPayload => key2.MakeRequest().With<string, string>(requestPayload))
                .Subscribe();

            var result = await key1.MakeRequest().With<string, string>("Initial Request").Capture();

            result.Error.ShouldBeNull();
            result.IsCompleted.ShouldBeTrue();
            result.Items.ShouldHaveSingleItem();
            result.Items.Single().ShouldBe("Response from Key2");
        }

        [Test]
        public async Task Handler_Failure_Is_Reported_To_FaultHub() {

            var key = "test_key";
            var handlerInvocationCount = 0;

            IObservable<string> FailingHandler(Unit _) {
                handlerInvocationCount++;
                return Observable.Throw<string>(new InvalidOperationException("Handler failed intentionally"));
            }

            using var _ = key.HandleRequest().With((Func<Unit, IObservable<string>>)FailingHandler).Subscribe();

            var result = await key.MakeRequest().With<Unit, string>(Unit.Default).Capture();

            handlerInvocationCount.ShouldBe(1);

            result.Error.ShouldBeOfType<InvalidOperationException>()
                .Message.ShouldBe("Handler failed intentionally");

            BusEvents.Count.ShouldBe(1,
                "The handler failure was not published to the FaultHub for system-wide observation.");
        }


    }
}
