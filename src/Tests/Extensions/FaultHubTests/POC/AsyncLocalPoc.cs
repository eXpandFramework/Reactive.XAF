using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.TestsLib.Common;

namespace Xpand.Extensions.Tests.FaultHubTests.POC{
    [TestFixture]
    public class AsyncLocalPoc {
        public static readonly AsyncLocal<string> TestContext = new();


        
        [Test]
        public async Task SelectMany_WithConcurrentItems_Context_Is_Preserved() {
            var capturedContexts = new ConcurrentBag<string>();

            var stream = Observable.Range(1, 3)
                .SelectMany(i => {
                    var innerStream = Observable.Timer(TimeSpan.FromMilliseconds(i * 20))
                        .Select(_ => TestContext.Value);
                    
                    return Observable.Using(
                        () => {
                            var contextValue = $"CONTEXT_FOR_{i}";
                            TestContext.Value = contextValue;
                            return Disposable.Create(() => TestContext.Value = null);
                        },
                        _ => innerStream
                    );
                })
                .Do(capturedContexts.Add);

            var observer = await stream.Test().AwaitDoneAsync(1.ToSeconds());

            observer.ItemCount.ShouldBe(3);
            capturedContexts.Skip(1).First().ShouldNotBe("CONTEXT_FOR_1");
        }
        
        [Test]
        public async Task Context_Set_BEFORE_SelectMany_Flows_Correctly_To_Concurrent_Operations() {
            var capturedContexts = new ConcurrentBag<string>();
            const string topLevelContext = "TOP_LEVEL_CONTEXT";

            var streamWithContext = Observable.Using(
                () => {
                    TestContext.Value = topLevelContext;
                    return Disposable.Create(() => TestContext.Value = null);
                },
                _ => Observable.Range(1, 3)
                    .SelectMany(i => Observable.Timer(TimeSpan.FromMilliseconds(i * 20))
                        .Select(_ => TestContext.Value)
                    )
                    .Do(capturedContexts.Add)
            );
            
            var testObserver = streamWithContext.Test();
            await testObserver.AwaitDoneAsync(1.ToSeconds());

            testObserver.CompletionCount.ShouldBe(1);
            

            capturedContexts.Count.ShouldBe(3);
            capturedContexts.All(c => c == topLevelContext).ShouldBeTrue();
        }
        [Test]
        public async Task PushStackFrame_Pattern_Inside_SelectMany_Works_Concurrently() {
            var capturedContexts = new ConcurrentDictionary<int, string>();

            var stream = Observable.Range(1, 3)
                .SelectMany(i => 
                    Observable.Timer(TimeSpan.FromMilliseconds(i * 20))
                        .Select(_ => TestContext.Value)
                        .Do(ctx => capturedContexts.TryAdd(i, ctx))
                        .SimulatePushStackFrame($"CONTEXT_FOR_{i}")
                );
            
            var testObserver = stream.Test();
            await testObserver.AwaitDoneAsync(1.ToSeconds());

            testObserver.CompletionCount.ShouldBe(1);
            

            var contextIsCorrupted = capturedContexts.Any(kvp => kvp.Value != $"CONTEXT_FOR_{kvp.Key}");
            contextIsCorrupted.ShouldBe(false, "The AsyncLocal context was not corrupted, which is unexpected in this concurrent scenario.");
        }        
        
        [Test]
        public async Task Context_Is_Preserved_In_Catch_Block_From_Concurrent_SelectMany_Operation() {
            string capturedContext = "CONTEXT_NOT_SET";
            const string topLevelContext = "TOP_LEVEL_CONTEXT";

            var streamWithContext = Observable.Using(
                () => {
                    TestContext.Value = topLevelContext;
                    return Disposable.Create(() => TestContext.Value = null);
                },
                _ => Observable.Return(1) 
                    .SelectMany(_ => 
                        Observable.Timer(TimeSpan.FromMilliseconds(20))
                            .SelectMany(_ => Observable.Throw<Unit>(new InvalidOperationException("Async Error")))
                    )
                    .Catch<Unit, Exception>(_ => {
                        capturedContext = TestContext.Value;
                        return Observable.Empty<Unit>();
                    })
            );
            
            var testObserver = streamWithContext.Test();
            await testObserver.AwaitDoneAsync(1.ToSeconds());

            testObserver.CompletionCount.ShouldBe(1);
            
            capturedContext.ShouldBe(topLevelContext);
        }
    }
    internal static class PocTestExtensions {
    
        internal static IObservable<T> SimulatePushStackFrame<T>(this IObservable<T> source, string context) {
            return Observable.Using(
                () => {
                    var originalContext = AsyncLocalPoc.TestContext.Value;
                    AsyncLocalPoc.TestContext.Value = context;
                    return Disposable.Create(() => AsyncLocalPoc.TestContext.Value = originalContext);
                },
                _ => source
            );
        }
    }
    }
