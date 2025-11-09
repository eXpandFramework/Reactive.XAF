using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.ErrorHandling;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<T[]> RollingBuffer<T>(this IObservable<T> source, TimeSpan buffering, IScheduler scheduler = null) {
            scheduler ??= TaskPoolScheduler.Default;
            return Observable.Create<T[]>(o => {
                var list = new LinkedList<Timestamped<T>>();
                return source.Timestamp(scheduler).Subscribe(tx => {
                    list.AddLast(tx);
                    while (scheduler.Now.Ticks > buffering.Ticks &&
                           (list.First!.Value.Timestamp < scheduler.Now.Subtract(buffering)))
                        list.RemoveFirst();
                    o.OnNext(list.Select(tx2 => tx2.Value).ToArray());
                }, o.OnError, o.OnCompleted);
            });
        }
        
        public static IObservable<IList<T>> BufferUntilInactive<T>(this IObservable<T> source, int seconds,IScheduler scheduler=null)
            => source.BufferUntilInactive(seconds.Seconds(),scheduler);
        
        public static IObservable<T[]> BufferWithInactivity<T>(this IObservable<T> source, TimeSpan inactivity, TimeSpan? maxBufferTime=null,IScheduler scheduler=null) {
            if (maxBufferTime.HasValue && maxBufferTime.Value < inactivity)
                throw new ArgumentException("maxBufferTime must be greater than or equal to inactivity", nameof(maxBufferTime));
            maxBufferTime ??= inactivity * 3;

            return Observable.Create<T[]>(observer => {
                var gate = new object();
                var buffer = new List<T>();
                var inactivityTimer = new SerialDisposable();
                var maxTimeTimer = new SerialDisposable();
                var subscription = new SerialDisposable();
                scheduler ??= Scheduler.Default;
                void Dump() {
                    if (buffer.Count <= 0) return;
                    var items = buffer.ToArray();
                    buffer.Clear();
                    observer.OnNext(items);
                }

                void ScheduleMaxTimeTimer() {
                    maxTimeTimer.Disposable = scheduler.Schedule(maxBufferTime.Value, () => {
                        lock (gate) {
                            Dump();
                            ScheduleMaxTimeTimer();
                        }
                    });
                }

                ScheduleMaxTimeTimer();

                subscription.Disposable = source.Subscribe(
                    onNext: x => {
                        lock (gate) {
                            buffer.Add(x);
                            inactivityTimer.Disposable = scheduler.Schedule(inactivity, () => {
                                lock (gate) {
                                    Dump();
                                }
                            });
                        }
                    },
                    onError: ex => {
                        lock (gate) {
                            Dump(); 
                            observer.OnError(ex);
                        }                        },
                    onCompleted: () => {
                        lock (gate) {
                            Dump();
                            observer.OnCompleted();
                        }
                    });

                return new CompositeDisposable(subscription, inactivityTimer, maxTimeTimer);
            });
        }
        
        public static IObservable<IList<T>> BufferUntilInactive<T>(this IObservable<T> source, TimeSpan delay,IScheduler scheduler=null) 
            => source.BufferWithInactivity(delay,scheduler:scheduler).Select(enumerable => enumerable.ToList());


        public static IObservable<IList<T>> Quiescent<T>(this IObservable<T> src, TimeSpan minimumInactivityPeriod, IScheduler scheduler=null) {
            scheduler ??= DefaultScheduler.Instance;
            var onoffs = src.SelectMany(_
                => Observable.Return(1, scheduler)
                    .Concat(Observable.Return(-1, scheduler).Delay(minimumInactivityPeriod,
                        scheduler)));
            var outstanding = onoffs.Scan(0, (total, delta) => total + delta);
            var zeroCrossings = outstanding.Where(total => total == 0);
            return src.Buffer(zeroCrossings);
        }
        public static IObservable<IList<T>> BufferUntilCompletionOrError<T>(this IObservable<T> source, params IObservable<T>[] sources) 
            => new[] { source }.Concat(sources).Select(s => s.Materialize()).Merge().ToList()
                .SelectMany(notifications => {
                    var items = notifications.Where(n => n.Kind == NotificationKind.OnNext).Select(n => n.Value).ToList();
                    var errors = notifications.Where(n => n.Kind == NotificationKind.OnError).Select(n => n.Exception!).ToList();
                    return !errors.Any() ? items.Observe() : items.Observe().Concat((sources.Length == 0 ? errors[0] : new AggregateException(errors)).Throw<List<T>>());
                });

        public static IObservable<TSource[]> BufferUntilCompleted<TSource>(this IObservable<TSource> source,bool skipEmpty=false) 
            => source.Buffer(Observable.Never<Unit>()).Where(sources => !skipEmpty || sources.Any()).Select(list => list.ToArray());
        
        
        public static IConnectableObservable<T> BufferUntilSubscribed<T>(this IObservable<T> source) => new BufferUntilSubscribedObservable<T>(source, ImmediateScheduler);

        class BufferUntilSubscribedObservable<T>(IObservable<T> source, IScheduler scheduler)
            : IConnectableObservable<T> {
            private readonly Subject<T> _liveEvents = new();
            private bool _observationsStarted;
            private Queue<T> _buffer = new();
            private readonly Lock _gate = new();

            public IDisposable Subscribe(IObserver<T> observer) {
                lock (_gate) {
                    if (_observationsStarted) {
                        return _liveEvents.Subscribe(observer);
                    }

                    _observationsStarted = true;

                    var bufferedEvents =
                        GetBuffers().Concat()
                            .Finally(
                                RemoveBuffer); 
                    return _liveEvents.Merge(bufferedEvents).Subscribe(observer);
                }
            }

            public IDisposable Connect() => source.Subscribe(OnNext, _liveEvents.OnError, _liveEvents.OnCompleted);

            private void RemoveBuffer() {
                lock (_gate) {
                    _buffer = null;
                }
            }

            
            
            
            
            private Queue<T> GetAndReplaceBuffer() {
                lock (_gate) {
                    if (_buffer == null) {
                        return null;
                    }

                    if (_buffer.Count == 0) {
                        _buffer = null;
                        return null;
                    }

                    var result = _buffer;
                    _buffer = new Queue<T>();
                    return result;
                }
            }
            
            private IEnumerable<IObservable<T>> GetBuffers() {
                while (GetAndReplaceBuffer() is{ } buffer) {
                    yield return buffer.ToObservable(scheduler);
                }
            }

            private void OnNext(T item) {
                lock (_gate) {
                    if (_buffer != null) {
                        _buffer.Enqueue(item);
                        return;
                    }
                }

                _liveEvents.OnNext(item);
            }
        }

        public static IObservable<IList<TSource>> BufferHistorical<TSource>(this IObservable<TSource> source, TimeSpan interval, TimeSpan replayDuration) 
            => source.Replay(replayed => Observable.Interval(interval)
                .SelectMany(_ => replayed.TakeUntil(Observable.Return(Unit.Default, Scheduler.CurrentThread)).ToList())
                .TakeUntil(replayed.LastOrDefaultAsync()), replayDuration, ImmediateScheduler);
        
        public static IObservable<IList<TSource>> BufferOmitEmpty<TSource>(this IObservable<TSource> observable, TimeSpan maxDelay, int maxBufferCount) 
            => observable.GroupByUntil(_ => 1, g => Observable.Timer(maxDelay).Merge(g.Skip(maxBufferCount - 1).Take(1).Select(_ => 1L)))
                .Select(x => x.ToArray())
                .Switch();

        public static IObservable<IList<TSource>> BufferByPredicate<TSource>(this IObservable<TSource> source, Predicate<TSource> startNewBufferPredicate) 
            => source.SelectMany(x => {
                    var subSequence = Observable.Return((Value: x, HasValue: true));
                    return startNewBufferPredicate(x) ? subSequence.Prepend((default, false)) : subSequence;
                })
                .GroupByUntil(_ => 0, g => g.SkipWhile(e => e.HasValue))
                .SelectMany(g => g.Where(e => e.HasValue).Select(e => e.Value).ToArray())
                .Where(w => w.Length > 0);

        public static IObservable<T> BufferWhen<T>(this IObservable<T> source, IObservable<object> signal,Func<IObservable<T>,IObservable<T>> selector=null) 
            => source.Publish(obs => obs.Buffer(signal).Take(1)
                .SelectMany(list => list.ToNowObservable().Concat(selector?.Invoke(obs)??obs)));
    }
    
    
}