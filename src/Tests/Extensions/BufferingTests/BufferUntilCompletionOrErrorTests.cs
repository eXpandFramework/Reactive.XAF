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
    }
}