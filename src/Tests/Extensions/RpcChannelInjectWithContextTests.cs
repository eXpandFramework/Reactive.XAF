using System;
using System.Linq;
using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Channels;
using Xpand.Extensions.Tests.FaultHubTests;

namespace Xpand.Extensions.Tests {
    [TestFixture]
    public class RpcChannelInjectWithContextTests : FaultHubTestBase {
        
        [Test]
        public void InjectWithContext_Falls_Back_To_Source_When_No_Handler_Is_Registered() {
            var key = nameof(InjectWithContext_Falls_Back_To_Source_When_No_Handler_Is_Registered);
            var context = "MyContext";
            var item = "OriginalItem";

            var observer = Observable.Return(item)
                .InjectWithContext(context, key)
                .Test();

            observer.AwaitDone(1.Seconds());
            
            observer.ItemCount.ShouldBe(1);
            observer.Items.Single().ShouldBe(item);
        }

        [Test]
        public void InjectWithContext_Replaces_Stream_And_Uses_Context() {
            
            var className = nameof(RpcChannelInjectWithContextTests);
            var methodName = nameof(InjectWithContext_Replaces_Stream_And_Uses_Context);
            var key = $"{className}.{methodName}";
            var context = 42;

            using var handler = key.InjectWithContext<int, string, string>(ctx => {
                ctx.ShouldBe(42);
                return Observable.Return("Injected");
            }).Subscribe();
            
            var observer = Observable.Return("Original")
                .InjectWithContext(context)
                .Test();
            
            observer.AwaitDone(1.Seconds());
            
            observer.Items.Single().ShouldBe("Injected");
            observer.ItemCount.ShouldBe(1);        }

        [Test]
        public void InjectWithContext_Builder_Syntax_Works() {
            var key = nameof(InjectWithContext_Builder_Syntax_Works);
            var context = "DriverRef";

            using var handler = key.Inject(typeof(RpcChannelInjectWithContextTests)).With<string>()
                .Using(ctx => {
                    ctx.ShouldBe("DriverRef");
                    return Observable.Return("BuilderWorks");
                })
                .Subscribe();

            var observer = Observable.Return("Original")
                .InjectWithContext(context, key)
                .Test();

            observer.AwaitDone(1.Seconds());

            observer.Items.Single().ShouldBe("BuilderWorks");
        }

        [Test]
        public void InjectWithContext_Handles_Tuples_Correctly() {
            var key = nameof(InjectWithContext_Handles_Tuples_Correctly);
            var context = (Id: 100, Status: "Active");

            using var handler = key.Inject(typeof(RpcChannelInjectWithContextTests)).With<(int Id, string Status)>()
                .Using(ctx => {
                    ctx.Id.ShouldBe(100);
                    ctx.Status.ShouldBe("Active");
                    return Observable.Return("TupleInjected");
                })
                .Subscribe();

            var observer = Observable.Return("Original")
                .InjectWithContext(context, key)
                .Test();

            observer.AwaitDone(1.Seconds());

            observer.Items.Single().ShouldBe("TupleInjected");
        }
        
        [Test]
        public void InjectWithContext_Is_Key_Specific() {
            
            var key1 = "MethodA";
            var key2 = "MethodB";
            var context = "Context";

            using var handler = key1.InjectWithContext<string, string,string>(_ => Observable.Return("Injected"))
                .Subscribe();

            var observer = Observable.Return("Original")
                .InjectWithContext(context, key2)
                .Test();
            
            observer.AwaitDone(1.Seconds());

            observer.Items.Single().ShouldBe("Original");
        }
    }
}