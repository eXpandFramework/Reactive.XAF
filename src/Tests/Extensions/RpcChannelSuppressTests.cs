using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Channels;
using Xpand.Extensions.Tests.FaultHubTests;
using Xpand.TestsLib.Common;

namespace Xpand.Extensions.Tests{
    [TestFixture]
    public class RpcChannelSuppressTests : FaultHubTestBase {
        [Test]
        public void Suppress_Operator_Filters_Item_When_Handler_Returns_True() {
            var key = "Suppress-true-key";
            var itemToSuppress = "item-should-be-Suppressd";
            
            var handler = key.Suppress<string,string>()
                .Subscribe();

            var source = Observable.Return(itemToSuppress);
            
            var observer = source.Suppress(key).Test();
            
            observer.AwaitDone(1.Seconds());
            observer.ItemCount.ShouldBe(0);
            observer.CompletionCount.ShouldBe(1);
            handler.Dispose();
        }
        
        [Test]
        public void Suppress_Operator_Emits_Item_When_Handler_Returns_False() {
            var key = "Suppress-false-key";
            var itemToEmit = "item-should-pass";

            var handler = key.Suppress<string,string>(_ => false)
                .Subscribe();

            var source = Observable.Return(itemToEmit);

            var observer = source.Suppress(key).Test();
            
            observer.AwaitDone(1.Seconds());
            observer.ItemCount.ShouldBe(1);
            observer.Items.Single().ShouldBe(itemToEmit);
            observer.CompletionCount.ShouldBe(1);
            handler.Dispose();
        }
        
        [Test]
        public void Suppress_Operator_Emits_Item_By_Default_When_No_Handler_Is_Present() {
            var key = "no-handler-key";
            var itemToEmit = "item-should-pass-default";

            var observer = Observable.Return(itemToEmit)
                .Suppress(key)
                .Test();
            
            observer.AwaitDone(1.Seconds());
            
            observer.ItemCount.ShouldBe(1);
            observer.Items.Single().ShouldBe(itemToEmit);
            observer.CompletionCount.ShouldBe(1);
        }
        
        [Test]
        public void TryWith_Returns_Default_Value_Immediately_If_No_Subscribers_Exist() {
            var key = "trywith-no-subscribers-key";
            var requestPayload = "payload";
            var defaultValue = "default-response";

            var observer = key.MakeRequest()
                .TryWith(requestPayload, defaultValue)
                .Test();

            observer.AwaitDone(100.Milliseconds());

            observer.ItemCount.ShouldBe(1);
            observer.Items.Single().ShouldBe(defaultValue);
            observer.CompletionCount.ShouldBe(1);
            observer.ErrorCount.ShouldBe(0);
        }
        
        [Test]
        public void Suppress_Operator_Works_With_Type_Keys() {
            var key = typeof(RpcChannelTests); 
            var itemToSuppress = "Suppress-me";

            var handler = key.Suppress<string,Type>()
                .Subscribe();

            var observer = Observable.Return(itemToSuppress)
                .Suppress(key)
                .Test();

            observer.AwaitDone(1.Seconds());

            observer.ItemCount.ShouldBe(0);
            observer.CompletionCount.ShouldBe(1);
            observer.ErrorCount.ShouldBe(0);
            handler.Dispose();
        }
        
        [Test]
        public void Suppress_Operator_Propagates_Handler_Exceptions() {
            var key = "error-handling-key";
            var exception = new InvalidOperationException("Decision failed");

            var handler = key.HandleRequest()
                .With<string, bool>(_ => Observable.Throw<bool>(exception))
                .Subscribe();

            var source = Observable.Return("item");

            var observer = source.Suppress(key).Test();

            observer.AwaitDone(1.Seconds());

            observer.ErrorCount.ShouldBe(1);
            observer.Errors.Single().ShouldBe(exception);
            observer.ItemCount.ShouldBe(0);
            
            handler.Dispose();
        }
        
        [Test]
        public void Suppress_Operator_Handles_Concurrent_Emissions_Correctly() {
            var key = "concurrent-Suppress-key";

            var handler = key.HandleRequest()
                .With<int, bool>(i => Observable.Return(i % 2 == 0).Delay(TimeSpan.FromMilliseconds(20)))
                .Subscribe();

            var count = 10;
            var source = Observable.Range(0, count);

            var observer = source.Suppress(key).Test();

            observer.AwaitDone(2.Seconds());

            observer.ItemCount.ShouldBe(count / 2);
            observer.Items.ShouldAllBe(i => i % 2 != 0);
            observer.CompletionCount.ShouldBe(1);
            
            handler.Dispose();
        }
        
        [Test]
        public async Task Suppress_Operator_Respects_Handler_Replacement() {
            var key = "replacement-key";
            var source = new Subject<string>();
            var observer = source.Suppress(key).Test();

            var handler1 = key.Suppress<string,string>(_ => false)
                .Subscribe();

            source.OnNext("Item-1");

            await observer.AwaitDoneAsync(1.ToSeconds());
            observer.Items.Last().ShouldBe("Item-1");

            handler1.Dispose();
            
            using var handler2 = key.HandleRequest()
                .With<string, bool>(_ => Observable.Return(true))
                .Subscribe();

            source.OnNext("Item-2");

            Observable.Timer(TimeSpan.FromMilliseconds(100)).Wait();
            
            observer.ItemCount.ShouldBe(1);
            observer.ErrorCount.ShouldBe(0);
        }
    
    
        [Test]
        public void Suppress_Operator_Stops_Ignoring_If_Channel_Resets_Detaching_Handler() {
            var key = "reset-detach-key";

            using var handler = key.Suppress<string,string>()
                .Subscribe();

            var source = new Subject<string>();
            var observer = source.Suppress(key).Test();

            source.OnNext("Item-1");
            observer.ItemCount.ShouldBe(0);

            RpcChannel.Reset();

            source.OnNext("Item-2");

            observer.ItemCount.ShouldBe(1);
            observer.Items.Single().ShouldBe("Item-2");
        }

        [Test]
        public void Suppress_Operator_Bypasses_Handler_If_Request_Type_Does_Not_Exactly_Match_Handler_Type() {
            var key = "variance-test-key";
            var itemToEmit = "string-item";

            var handler = key.Suppress<object,Object>() 
                .Subscribe();

            var observer = Observable.Return(itemToEmit)
                .Suppress(key) 
                .Test();

            observer.AwaitDone(1.Seconds());

            observer.ItemCount.ShouldBe(1);
            observer.Items.Single().ShouldBe(itemToEmit);
            handler.Dispose();
        }
    }
}