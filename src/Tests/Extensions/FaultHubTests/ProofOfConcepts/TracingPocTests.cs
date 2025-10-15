using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using NUnit.Framework;
using Shouldly;
using System.Reactive.Concurrency;

// ReSharper disable once CheckNamespace
namespace Xpand.Extensions.Tests.FaultHubTests.Final
{
    public class ExceptionWithContext(Exception originalException, ImmutableList<string> contextPath)
        : Exception(originalException.Message, originalException)
    {
        public Exception OriginalException { get; } = originalException;
        public ImmutableList<string> ContextPath { get; } = contextPath;
    }

    public static class ObservableTracingExtensions
    {
        
        private static readonly AsyncLocal<ImmutableList<string>> NotificationContext = new();

        private class PushObserver<T>(IObserver<T> downstream, string name) : IObserver<T> {
            private readonly ImmutableList<string> _parentPath = NotificationContext.Value ?? ImmutableList<string>.Empty;

            public void OnNext(T value)
            {
                var originalContext = NotificationContext.Value;
                try
                {
                    NotificationContext.Value = _parentPath.Add(name);
                    downstream.OnNext(value);
                }
                finally
                {
                    NotificationContext.Value = originalContext;
                }
            }

            public void OnError(Exception error)
            {
                if (error is ExceptionWithContext contextException)
                {
                    var newPath = contextException.ContextPath.Add(name);
                    downstream.OnError(new ExceptionWithContext(contextException.OriginalException, newPath));
                }
                else
                {
                    var newPath = _parentPath.Add(name);
                    downstream.OnError(new ExceptionWithContext(error, newPath));
                }
            }

            public void OnCompleted() => downstream.OnCompleted();
        }

        public static IObservable<T> Push<T>(this IObservable<T> source, string name) 
            => Observable.Create<T>(observer => source.Subscribe(new PushObserver<T>(observer, name)));

        public static IObservable<T> Chain<T>(this IObservable<T> source, Action<Exception, ImmutableList<string>> errorHandler)
        {
            return source.Catch<T, Exception>(ex =>
            {
                if (ex is ExceptionWithContext contextException)
                {
                    // The path is built outwards from the source, so we reverse it for a conventional stack trace order.
                    errorHandler(contextException.OriginalException, contextException.ContextPath);
                }
                else
                {
                    errorHandler(ex, ImmutableList<string>.Empty);
                }
                return Observable.Empty<T>();
            });
        }


    }

    [TestFixture]
    public class ObservableTracingTests
    {
        [Test]
        public void Chain_WhenMergedStreamErrors_CapturesCorrectPathExcludingNonFailingStream()
        {
            var streamA = new Subject<int>();
            var streamB = new Subject<int>();
            var testException = new Exception("Test Failure");

            Exception capturedException = null;
            ImmutableList<string> capturedPath = null;
            var errorHandledSignal = new ManualResetEventSlim(false);

            void ErrorHandler(Exception ex, ImmutableList<string> path)
            {
                capturedException = ex;
                capturedPath = path;
                errorHandledSignal.Set();
            }

            var query = streamA.Push("StreamA")
                .Merge(streamB.Push("StreamB"))
                .Push("Projection")
                .Chain(ErrorHandler);

            using (query.Subscribe())
            {
                streamA.OnNext(1);
                streamB.OnNext(2);
                streamA.OnNext(3);
                
                streamB.OnError(testException);
                streamA.OnCompleted();
                
                errorHandledSignal.Wait(TimeSpan.FromSeconds(1));
            }

            capturedException.ShouldNotBeNull();
            capturedException.ShouldBe(testException);

            capturedPath.ShouldNotBeNull();
            var pathArray = capturedPath.ToArray();
            
            pathArray.ShouldBe(["StreamB", "Projection"]);
            pathArray.ShouldNotContain("StreamA");
        }
        
        [Test]
        public void Chain_WhenInnerStreamFromSelectManyErrors_CapturesCorrectHierarchicalPath()
        {
            var source = new Subject<int>();
            var testException = new Exception("Inner Failure");

            Exception capturedException = null;
            ImmutableList<string> capturedPath = null;
            var errorHandledSignal = new ManualResetEventSlim(false);

            void ErrorHandler(Exception ex, ImmutableList<string> path) {
                capturedException = ex;
                capturedPath = path;
                errorHandledSignal.Set();
            }

            var query = source
                .Push("OuterStream")
                .SelectMany(i => {
                    if (i == 1) return Observable.Return($"Success-{i}");
                    return Observable.Throw<string>(testException).Push("InnerStream-Failure");
                })
                .Chain(ErrorHandler);

            using (query.Subscribe())
            {
                source.OnNext(1);
                source.OnNext(2);
                errorHandledSignal.Wait(TimeSpan.FromSeconds(1));
            }

            capturedException.ShouldNotBeNull();
            capturedException.ShouldBe(testException);

            capturedPath.ShouldNotBeNull();
            capturedPath.ToArray().ShouldBe(["OuterStream", "InnerStream-Failure"]);
        }
        
        [Test]
        public void Chain_WithStandardRetry_CorrectlyRetriesAndCapturesFinalPath()
        {
            var testException = new Exception("Retry Failure");
            var subscriptionCount = 0;

            Exception capturedException = null;
            ImmutableList<string> capturedPath = null;
            var errorHandledSignal = new ManualResetEventSlim(false);

            void ErrorHandler(Exception ex, ImmutableList<string> path) {
                capturedException = ex;
                capturedPath = path;
                errorHandledSignal.Set();
            }
            
            var queryWithChainBeforeRetry = Observable.Defer(() => {
                    Interlocked.Increment(ref subscriptionCount);
                    return Observable.Throw<int>(testException);
                })
                .Push("SourceStream")
                .Chain(ErrorHandler)
                .Retry(3);

            using(queryWithChainBeforeRetry.Subscribe(_ => {}, _ => {})) {
                errorHandledSignal.Wait(TimeSpan.FromSeconds(1));
            }

            subscriptionCount.ShouldBe(1);
            capturedException.ShouldBe(testException);
            capturedPath.ShouldNotBeNull();
            capturedPath.Single().ShouldBe("SourceStream");
        }

        [Test]
        public void Chain_WhenObserveOnIsUsed_CorrectlyFlowsContextAcrossThreads()
        {
            var source = new Subject<int>();
            var testException = new Exception("Concurrency Failure");

            Exception capturedException = null;
            ImmutableList<string> capturedPath = null;
            var errorHandledSignal = new ManualResetEventSlim(false);

            void ErrorHandler(Exception ex, ImmutableList<string> path) {
                capturedException = ex;
                capturedPath = path;
                errorHandledSignal.Set();
            }

            var query = source
                .Push("FrameA")
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Push("FrameB")
                .Chain(ErrorHandler);

            using (query.Subscribe())
            {
                source.OnError(testException);
                errorHandledSignal.Wait(TimeSpan.FromSeconds(2));
            }

            errorHandledSignal.IsSet.ShouldBeTrue("The error handler was never called.");
            capturedException.ShouldBe(testException);
            capturedPath.ShouldNotBeNull();
            capturedPath.ToArray().ShouldBe(["FrameA", "FrameB"]);
        }

        private class TestResource : IDisposable {
            public bool IsDisposed { get; private set; }
            public void Dispose() => IsDisposed = true;
        }

        [Test]
        public void Chain_WhenErrorOccursInUsing_CorrectlyCapturesPathAndDisposesResource()
        {
            var source = new Subject<int>();
            var testException = new Exception("Using Failure");
            TestResource resource = null;

            Exception capturedException = null;
            ImmutableList<string> capturedPath = null;
            var errorHandledSignal = new ManualResetEventSlim(false);

            void ErrorHandler(Exception ex, ImmutableList<string> path) {
                capturedException = ex;
                capturedPath = path;
                errorHandledSignal.Set();
            }

            var query = Observable.Using(
                    () => {
                        resource = new TestResource();
                        return resource;
                    },
                    _ => source.Push("InsideUsing")
                )
                .Push("OutsideUsing")
                .Chain(ErrorHandler);

            using (query.Subscribe())
            {
                source.OnError(testException);
                errorHandledSignal.Wait(TimeSpan.FromSeconds(1));
            }

            errorHandledSignal.IsSet.ShouldBeTrue("The error handler was never called.");
            resource.ShouldNotBeNull();
            resource.IsDisposed.ShouldBeTrue();
            capturedException.ShouldBe(testException);
            capturedPath.ShouldNotBeNull();
            capturedPath.ToArray().ShouldBe(["InsideUsing", "OutsideUsing"]);
        }

        [Test]
        public void Chain_WithNestedSelectMany_CapturesFullHierarchicalPath()
        {
            var source = new Subject<string>();
            var testException = new Exception("Deep Failure");

            Exception capturedException = null;
            ImmutableList<string> capturedPath = null;
            var errorHandledSignal = new ManualResetEventSlim(false);

            void ErrorHandler(Exception ex, ImmutableList<string> path) {
                capturedException = ex;
                capturedPath = path;
                errorHandledSignal.Set();
            }

            var query = source
                .Push("Level1")
                .SelectMany(outerId => 
                    Observable.Return($"{outerId}-subtask")
                        .Push("Level2")
                        .SelectMany(_ => {
                            if (outerId == "request-1") {
                                return Observable.Return("Success");
                            }
                            // This is the innermost stream where the error originates.
                            return Observable.Throw<string>(testException)
                                .Push("Level3-Failure");
                        })
                )
                .Chain(ErrorHandler);

            using (query.Subscribe())
            {
                source.OnNext("request-1"); // This request will succeed.
                source.OnNext("request-2"); // This request will trigger the failure.
            
                errorHandledSignal.Wait(TimeSpan.FromSeconds(1));
            }

            errorHandledSignal.IsSet.ShouldBeTrue("The error handler was never called.");
            capturedException.ShouldBe(testException);
            capturedPath.ShouldNotBeNull();
        
            // The path should correctly reflect the full hierarchy from the outermost
            // operator to the operator where the error was thrown.
            capturedPath.ToArray().ShouldBe(["Level1", "Level2", "Level3-Failure"]);
        }
        [Test]
        public void Chain_WhenTimeoutOperatorTriggers_CapturesCorrectPath()
        {
            var source = new Subject<int>();
            Exception capturedException = null;
            ImmutableList<string> capturedPath = null;
            var errorHandledSignal = new ManualResetEventSlim(false);

            void ErrorHandler(Exception ex, ImmutableList<string> path) {
                capturedException = ex;
                capturedPath = path;
                errorHandledSignal.Set();
            }

            var query = source
                .Push("BeforeTimeout")
                .Timeout(TimeSpan.FromMilliseconds(50))
                .Push("AfterTimeout")
                .Chain(ErrorHandler);

            using (query.Subscribe(
                       _ => { /* A value is successfully received */ },
                       _ => {  }
                   ))
            {
                // This value will pass through successfully.
                source.OnNext(1); 
            
                // Now we simulate a pause longer than the timeout.
                // The Timeout operator will trigger and generate its own exception.
                Thread.Sleep(100);

                errorHandledSignal.Wait(TimeSpan.FromSeconds(2));
            }

            errorHandledSignal.IsSet.ShouldBeTrue("The error handler was never called.");
            capturedException.ShouldBeOfType<TimeoutException>();
            capturedPath.ShouldNotBeNull();
        
            // The standard Rx Timeout operator generates a new exception and does not propagate
            // context from its upstream source. Therefore, the tracing mechanism can only
            // capture the path from the point of the timeout onwards.
            capturedPath.ToArray().ShouldBe(["AfterTimeout"]);
            
        }
        
        [Test]
        public void Chain_WhenErrorOriginatesInNonInstrumentedStream_CapturesEmptyPath()
        {
            var testException = new Exception("Non-Instrumented Failure");
            Exception capturedException = null;
            ImmutableList<string> capturedPath = null;
            var errorHandledSignal = new ManualResetEventSlim(false);

            void ErrorHandler(Exception ex, ImmutableList<string> path) {
                capturedException = ex;
                capturedPath = path;
                errorHandledSignal.Set();
            }

            var query = Observable.Throw<int>(testException)
                .Chain(ErrorHandler);

            using (query.Subscribe())
            {
                errorHandledSignal.Wait(TimeSpan.FromSeconds(1));
            }

            capturedException.ShouldBe(testException);
            capturedPath.ShouldNotBeNull();
            capturedPath.ShouldBeEmpty();
        }
        
        [Test]
        public void Chain_WhenSourceCompletesSuccessfully_PropagatesOnCompleted()
        {
            var wasCompleted = false;
            var errorHandlerWasCalled = false;

            void ErrorHandler(Exception ex, ImmutableList<string> path) {
                errorHandlerWasCalled = true;
            }

            var query = Observable.Return(1)
                .Push("SourceStream")
                .Chain(ErrorHandler);

            using (query.Subscribe(
                       _ => { },
                       () => wasCompleted = true
                   ))
            { }

            wasCompleted.ShouldBeTrue("OnCompleted was not propagated by the Chain operator.");
            errorHandlerWasCalled.ShouldBeFalse("Error handler was called on a successful stream.");
        }
        
        [Test]
        public void Chain_WhenSubscribedSequentially_DoesNotPolluteContext()
        {
            var errorHandledSignal1 = new ManualResetEventSlim(false);
            ImmutableList<string> capturedPath1 = null;
            void ErrorHandler1(Exception ex, ImmutableList<string> path) {
                capturedPath1 = path;
                errorHandledSignal1.Set();
            }

            var errorHandledSignal2 = new ManualResetEventSlim(false);
            ImmutableList<string> capturedPath2 = null;
            void ErrorHandler2(Exception ex, ImmutableList<string> path) {
                capturedPath2 = path;
                errorHandledSignal2.Set();
            }

            var query1 = Observable.Throw<int>(new Exception("Failure 1"))
                .Push("Stream1")
                .Chain(ErrorHandler1);

            var query2 = Observable.Throw<int>(new Exception("Failure 2"))
                .Push("Stream2")
                .Chain(ErrorHandler2);

            // Act
            using (query1.Subscribe()) {
                errorHandledSignal1.Wait(TimeSpan.FromSeconds(1));
            }

            using (query2.Subscribe()) {
                errorHandledSignal2.Wait(TimeSpan.FromSeconds(1));
            }

            // Assert
            capturedPath1.ShouldNotBeNull();
            capturedPath1.Single().ShouldBe("Stream1");

            capturedPath2.ShouldNotBeNull();
            capturedPath2.Single().ShouldBe("Stream2", "Context from the first stream leaked into the second.");
        }
    }
}