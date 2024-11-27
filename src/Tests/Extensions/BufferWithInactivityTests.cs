using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Tests{
    [TestFixture]
    public class BufferWithInactivityTests {
        [Test]
        public void BuffersOnInactivity() {
            var scheduler = new TestScheduler();
            var source = scheduler.CreateColdObservable(
                ReactiveTest.OnNext(100, 1),
                ReactiveTest.OnNext(200, 2),
                ReactiveTest.OnNext(400, 3),
                ReactiveTest.OnCompleted<int>(500)
            );

            var results = scheduler.CreateObserver<IEnumerable<int>>();

            source.BufferWithInactivity(TimeSpan.FromTicks(150), scheduler: scheduler)
                .Subscribe(results);

            scheduler.Start();

            Assert.That(results.Messages.Select(m => m.Value.Kind), Is.EquivalentTo(new[] {
                NotificationKind.OnNext,
                NotificationKind.OnNext,
                NotificationKind.OnCompleted
            }));

            Assert.That(results.Messages[0].Value.Value, Is.EquivalentTo(new[] { 1, 2 }));
            Assert.That(results.Messages[1].Value.Value, Is.EquivalentTo(new[] { 3 }));
        }

        [Test]
        public void BuffersOnMaxBufferTime() {
            var scheduler = new TestScheduler();
            var source = scheduler.CreateColdObservable(
                ReactiveTest.OnNext(100, 1),
                ReactiveTest.OnNext(200, 2),
                ReactiveTest.OnNext(400, 3),
                ReactiveTest.OnCompleted<int>(500)
            );

            var results = scheduler.CreateObserver<IEnumerable<int>>();

            source.BufferWithInactivity(
                    TimeSpan.FromTicks(150),
                    TimeSpan.FromTicks(300),
                    scheduler
                )
                .Subscribe(results);

            scheduler.Start();

            Assert.That(results.Messages.Select(m => m.Value.Kind), Is.EquivalentTo(new[] {
                NotificationKind.OnNext,
                NotificationKind.OnNext,
                NotificationKind.OnCompleted
            }));

            Assert.That(results.Messages[0].Value.Value, Is.EquivalentTo(new[] { 1, 2 }));
            Assert.That(results.Messages[1].Value.Value, Is.EquivalentTo(new[] { 3 }));
        }

        [Test]
        public void HandlesOnError() {
            var scheduler = new TestScheduler();
            var ex = new Exception("Test error");
            var source = scheduler.CreateColdObservable(
                ReactiveTest.OnNext(100, 1),
                ReactiveTest.OnNext(200, 2),
                ReactiveTest.OnError<int>(300, ex)
            );

            var results = scheduler.CreateObserver<IEnumerable<int>>();

            source.BufferWithInactivity(TimeSpan.FromTicks(150), scheduler: scheduler)
                .Subscribe(results);

            scheduler.Start();

            Assert.That(results.Messages.Count, Is.EqualTo(2));
            Assert.That(results.Messages[0].Value.Value, Is.EquivalentTo(new[] { 1, 2 }));
            Assert.That(results.Messages[1].Value.Exception, Is.EqualTo(ex));
        }

        [Test]
        public void HandlesOnCompleted() {
            var scheduler = new TestScheduler();
            var source = scheduler.CreateColdObservable(
                ReactiveTest.OnNext(100, 1),
                ReactiveTest.OnNext(200, 2),
                ReactiveTest.OnCompleted<int>(300)
            );

            var results = scheduler.CreateObserver<IEnumerable<int>>();

            source.BufferWithInactivity(TimeSpan.FromTicks(150), scheduler: scheduler)
                .Subscribe(results);

            scheduler.Start();

            Assert.That(results.Messages.Count, Is.EqualTo(2));
            Assert.That(results.Messages[0].Value.Value, Is.EquivalentTo(new[] { 1, 2 }));
            Assert.That(results.Messages[1].Value.Kind, Is.EqualTo(NotificationKind.OnCompleted));
        }

        [Test]
        public void DoesNotEmitEmptyBuffers() {
            var scheduler = new TestScheduler();
            var source = scheduler.CreateColdObservable(
                ReactiveTest.OnNext(100, 1),
                ReactiveTest.OnCompleted<int>(200)
            );

            var results = scheduler.CreateObserver<IEnumerable<int>>();

            source.BufferWithInactivity(TimeSpan.FromTicks(150), scheduler: scheduler)
                .Subscribe(results);

            scheduler.Start();

            Assert.That(results.Messages.Count, Is.EqualTo(2));
            Assert.That(results.Messages[0].Value.Value, Is.EquivalentTo(new[] { 1 }));
            Assert.That(results.Messages[1].Value.Kind, Is.EqualTo(NotificationKind.OnCompleted));
        }
    }
}