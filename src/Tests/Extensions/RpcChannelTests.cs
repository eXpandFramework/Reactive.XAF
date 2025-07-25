using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive;
using MemoryCache = Microsoft.Extensions.Caching.Memory.MemoryCache;

namespace Xpand.Extensions.Tests {
    [TestFixture]
    public class RpcChannelTests {
        [SetUp]
        public void Setup() {

            var rpcChannelType = typeof(RpcChannel);
            var fieldInfo = rpcChannelType.GetField("Channels", BindingFlags.Static | BindingFlags.NonPublic);
            fieldInfo.ShouldNotBeNull();
            
            var channelsCache = fieldInfo.GetValue(null) as MemoryCache;
            channelsCache.ShouldNotBeNull();
            
            channelsCache.Compact(1.0);
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
    }
}