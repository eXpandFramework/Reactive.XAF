using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Xpand.Extensions.Reactive.Subjects {
    public class RollingReplaySubject {
        public static RollingReplaySubject<TSource, long> Create<TSource>(
            TimeSpan bufferClearingInterval) {
            return
                new RollingReplaySubject<TSource, long>(
                    Observable.Interval(bufferClearingInterval));
        }

        public static RollingReplaySubject<TSource, long> Create<TSource>(
            TimeSpan bufferClearingInterval, IScheduler scheduler) {
            return
                new RollingReplaySubject<TSource, long>(
                    Observable.Interval(bufferClearingInterval, scheduler));
        }

        protected class NopSubject<TSource> : ISubject<TSource> {
            public static readonly NopSubject<TSource> Default = new();

            public void OnCompleted() { }

            public void OnError(Exception error) { }

            public void OnNext(TSource value) { }

            public IDisposable Subscribe(IObserver<TSource> observer) {
                return Disposable.Empty;
            }
        }
    }

    public class RollingReplaySubject<TSource, TBufferClearing> : RollingReplaySubject, ISubject<TSource> {
        private readonly ReplaySubject<IObservable<TSource>> _subjects;
        private readonly IObservable<TSource> _concatenatedSubjects;
        private ISubject<TSource> _currentSubject;
        private readonly IDisposable _bufferClearingHandle;
        private readonly object _gate = new();

        public RollingReplaySubject(IObservable<TBufferClearing> bufferClearing) {
            _bufferClearingHandle = bufferClearing.Synchronize(_gate).Subscribe(_ => Clear());
            _subjects = new ReplaySubject<IObservable<TSource>>(1);
            _concatenatedSubjects = _subjects.Concat();
            _currentSubject = new ReplaySubject<TSource>();
            _subjects.OnNext(_currentSubject);
        }

        private void Clear() {
            _currentSubject.OnCompleted();
            _currentSubject = new ReplaySubject<TSource>();
            _subjects.OnNext(_currentSubject);
        }

        public void OnNext(TSource value) {
            lock (_gate) {
                _currentSubject.OnNext(value);
            }
        }

        public void OnError(Exception error) {
            lock (_gate) {
                _currentSubject.OnError(error);
                _currentSubject = NopSubject<TSource>.Default;
                _bufferClearingHandle.Dispose();
            }
        }

        public void OnCompleted() {
            lock (_gate) {
                _currentSubject.OnCompleted();
                _subjects.OnCompleted();
                _currentSubject = NopSubject<TSource>.Default;
                _bufferClearingHandle.Dispose();
            }
        }

        public IDisposable Subscribe(IObserver<TSource> observer) {
            return _concatenatedSubjects.Subscribe(observer);
        }
    }
}