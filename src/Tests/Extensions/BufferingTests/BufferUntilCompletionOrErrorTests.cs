using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Tests.BufferingTests{
    [TestFixture]
    public class BufferUntilCompletionOrErrorTests {
        private TestScheduler _scheduler;

        [SetUp]
        public void SetUp() {
            _scheduler = new TestScheduler();
        }

        [Test]
        public void ShouldBufferAllItemsAndEmitOnCompletion() {
            var source = _scheduler.CreateColdObservable(
                new Recorded<Notification<int>>(100, Notification.CreateOnNext(1)),
                new Recorded<Notification<int>>(200, Notification.CreateOnNext(2)),
                new Recorded<Notification<int>>(300, Notification.CreateOnNext(3)),
                new Recorded<Notification<int>>(400, Notification.CreateOnCompleted<int>())
            );

            var observer = _scheduler.CreateObserver<IList<int>>();

            source.BufferUntilCompletionOrError().Subscribe(observer);
            _scheduler.Start();

            observer.Messages.Count.ShouldBe(2);

            observer.Messages[0].Value.Kind.ShouldBe(NotificationKind.OnNext);
            observer.Messages[0].Value.Value.ShouldBe([1, 2, 3]);

            observer.Messages[1].Value.Kind.ShouldBe(NotificationKind.OnCompleted);
        }

        [Test]
        public void ShouldEmitBufferedItemsThenErrorWhenSourceErrors() {
            var testException = new InvalidOperationException("Test Exception");
            var source = _scheduler.CreateColdObservable(
                new Recorded<Notification<int>>(100, Notification.CreateOnNext(1)),
                new Recorded<Notification<int>>(200, Notification.CreateOnNext(2)),
                new Recorded<Notification<int>>(300, Notification.CreateOnError<int>(testException))
            );

            var observer = _scheduler.CreateObserver<IList<int>>();

            source.BufferUntilCompletionOrError().Subscribe(observer);
            _scheduler.Start();

            observer.Messages.Count.ShouldBe(2);

            observer.Messages[0].Value.Kind.ShouldBe(NotificationKind.OnNext);
            observer.Messages[0].Value.Value.ShouldBe([1, 2]);

            observer.Messages[1].Value.Kind.ShouldBe(NotificationKind.OnError);
            observer.Messages[1].Value.Exception.ShouldBe(testException);
        }

        [Test]
        public void ShouldEmitEmptyListWhenSourceIsEmptyAndCompletes() {
            var source = _scheduler.CreateColdObservable(
                new Recorded<Notification<int>>(100, Notification.CreateOnCompleted<int>())
            );

            var observer = _scheduler.CreateObserver<IList<int>>();

            source.BufferUntilCompletionOrError().Subscribe(observer);
            _scheduler.Start();

            observer.Messages.Count.ShouldBe(2);

            observer.Messages[0].Value.Kind.ShouldBe(NotificationKind.OnNext);
            observer.Messages[0].Value.Value.ShouldBeEmpty();

            observer.Messages[1].Value.Kind.ShouldBe(NotificationKind.OnCompleted);
        }

        [Test]
        public void ShouldEmitEmptyListThenErrorWhenSourceErrorsImmediately() {
            var testException = new InvalidOperationException("Immediate Exception");
            var source = _scheduler.CreateColdObservable(
                new Recorded<Notification<int>>(100, Notification.CreateOnError<int>(testException))
            );

            var observer = _scheduler.CreateObserver<IList<int>>();

            source.BufferUntilCompletionOrError().Subscribe(observer);
            _scheduler.Start();

            observer.Messages.Count.ShouldBe(2);
        
            observer.Messages[0].Value.Kind.ShouldBe(NotificationKind.OnNext);
            observer.Messages[0].Value.Value.ShouldBeEmpty();

            observer.Messages[1].Value.Kind.ShouldBe(NotificationKind.OnError);
            observer.Messages[1].Value.Exception.ShouldBe(testException);
        }
        
        [Test]
        public void ShouldBeThreadSafeWithConcurrentSource() {
            const int itemCount = 10000;
            var concurrentSource = Enumerable.Range(0, itemCount)
                .ToObservable(ThreadPoolScheduler.Instance);

            var result = concurrentSource.BufferUntilCompletionOrError().Wait();

            result.Count.ShouldBe(itemCount);
            result.ShouldBeUnique();
        }
        
        [Test]
        public void ShouldBufferAllItemsFromMultipleSourcesAndEmitOnCompletion() {
            var source1 = _scheduler.CreateColdObservable(
                new Recorded<Notification<int>>(100, Notification.CreateOnNext(1)),
                new Recorded<Notification<int>>(300, Notification.CreateOnCompleted<int>())
            );
            var source2 = _scheduler.CreateColdObservable(
                new Recorded<Notification<int>>(200, Notification.CreateOnNext(2)),
                new Recorded<Notification<int>>(400, Notification.CreateOnCompleted<int>())
            );

            var observer = _scheduler.CreateObserver<IList<int>>();

            source1.BufferUntilCompletionOrError(source2).Subscribe(observer);
            _scheduler.Start();

            observer.Messages.Count.ShouldBe(2);
            observer.Messages[0].Value.Value.ShouldBe([1, 2]);
            observer.Messages[1].Value.Kind.ShouldBe(NotificationKind.OnCompleted);
        }

        [Test]
        public void ShouldBufferItemsFromAllSourcesAndEmitSingleError() {
            var testException = new InvalidOperationException("Secondary Exception");
            var source1 = _scheduler.CreateColdObservable(
                new Recorded<Notification<int>>(100, Notification.CreateOnNext(1)),
                new Recorded<Notification<int>>(300, Notification.CreateOnCompleted<int>())
            );
            var source2 = _scheduler.CreateColdObservable(
                new Recorded<Notification<int>>(200, Notification.CreateOnNext(2)),
                new Recorded<Notification<int>>(400, Notification.CreateOnError<int>(testException))
            );

            var observer = _scheduler.CreateObserver<IList<int>>();

            source1.BufferUntilCompletionOrError(source2).Subscribe(observer);
            _scheduler.Start();

            observer.Messages.Count.ShouldBe(2);
            observer.Messages[0].Value.Value.ShouldBe([1, 2]);
            observer.Messages[1].Value.Kind.ShouldBe(NotificationKind.OnError);
            observer.Messages[1].Value.Exception.ShouldBeOfType<AggregateException>()
                .InnerExceptions.Single().ShouldBe(testException);
        }
        
        [Test]
        public void ShouldBufferItemsFromAllSourcesAndEmitAggregateError() {
            var ex1 = new InvalidOperationException("Primary Exception");
            var ex2 = new ArgumentException("Secondary Exception");
            var source1 = _scheduler.CreateColdObservable(
                new Recorded<Notification<int>>(100, Notification.CreateOnNext(1)),
                new Recorded<Notification<int>>(300, Notification.CreateOnError<int>(ex1))
            );
            var source2 = _scheduler.CreateColdObservable(
                new Recorded<Notification<int>>(200, Notification.CreateOnNext(2)),
                new Recorded<Notification<int>>(400, Notification.CreateOnError<int>(ex2))
            );

            var observer = _scheduler.CreateObserver<IList<int>>();

            source1.BufferUntilCompletionOrError(source2).Subscribe(observer);
            _scheduler.Start();

            observer.Messages.Count.ShouldBe(2);
            observer.Messages[0].Value.Value.ShouldBe([1, 2]);
            observer.Messages[1].Value.Kind.ShouldBe(NotificationKind.OnError);
            observer.Messages[1].Value.Exception.ShouldBeOfType<AggregateException>()
                .InnerExceptions.ShouldContain(ex1);
            observer.Messages[1].Value.Exception.ShouldBeOfType<AggregateException>()
                .InnerExceptions.ShouldContain(ex2);
        }

        [Test]
        public void ShouldEmitEmptyListWhenAllSourcesAreEmptyAndComplete() {
            var source1 = _scheduler.CreateColdObservable(
                new Recorded<Notification<int>>(100, Notification.CreateOnCompleted<int>())
            );
            var source2 = _scheduler.CreateColdObservable(
                new Recorded<Notification<int>>(200, Notification.CreateOnCompleted<int>())
            );

            var observer = _scheduler.CreateObserver<IList<int>>();

            source1.BufferUntilCompletionOrError(source2).Subscribe(observer);
            _scheduler.Start();

            observer.Messages.Count.ShouldBe(2);
            observer.Messages[0].Value.Value.ShouldBeEmpty();
            observer.Messages[1].Value.Kind.ShouldBe(NotificationKind.OnCompleted);
        }
        
        [Test]
        public void ShouldBeThreadSafeWithMultipleConcurrentSources() {
            const int sourceCount = 5;
            const int itemsPerSource = 2000;

            var sources = Enumerable.Range(0, sourceCount)
                .Select(i => Enumerable.Range(i * itemsPerSource, itemsPerSource)
                    .ToObservable(ThreadPoolScheduler.Instance))
                .ToArray();

            var primarySource = sources.First();
            var additionalSources = sources.Skip(1).ToArray();

            var result = primarySource.BufferUntilCompletionOrError(additionalSources).Wait();

            result.Count.ShouldBe(sourceCount * itemsPerSource);
            result.ShouldBeUnique();
        }
    }
}