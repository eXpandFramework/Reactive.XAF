﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Xpand.Extensions.Numeric;

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
        
        // public static IObservable<IList<T>> BufferUntilInactive<T>(this IObservable<T> source, TimeSpan delay,IScheduler scheduler=null)
            // => source.BufferUntilInactive(delay,scheduler);
            // => source.BufferUntilInactive(delay,window => window.ToList(),scheduler);
            // => source.Quiescent(delay,scheduler);
        
        public static IObservable<IList<T>> BufferUntilInactive<T>(this IObservable<T> source, int seconds,IScheduler scheduler=null)
            => source.BufferUntilInactive(seconds.Seconds());
            // => source.BufferUntilInactive(seconds.Seconds(),window => window.ToList(),scheduler);
            // => source.Quiescent(seconds.Seconds(),scheduler);
        
            public static IObservable<IEnumerable<T>> BufferWithInactivity<T>(this IObservable<T> source, TimeSpan inactivity, TimeSpan? maxBufferTime=null,IScheduler scheduler=null) {
                if (maxBufferTime.HasValue && maxBufferTime.Value < inactivity)
                    throw new ArgumentException("maxBufferTime must be greater than or equal to inactivity", nameof(maxBufferTime));
                maxBufferTime ??= inactivity * 3;

                return Observable.Create<IEnumerable<T>>(observer => {
                    var gate = new object();
                    var buffer = new List<T>();
                    var inactivityTimer = new SerialDisposable();
                    var maxTimeTimer = new SerialDisposable();
                    var subscription = new SerialDisposable();
                    scheduler ??= Scheduler.ThreadPool;
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
public static IObservable<IList<T>> BufferUntilInactive<T>(this IObservable<T> source, TimeSpan delay,IScheduler scheduler=null) {
            return source.BufferWithInactivity(delay,scheduler:scheduler).Select(enumerable => enumerable.ToList());
            // var publish = source.Publish(p => {
            //     var closes = p.Throttle(delay);
            //     if (maxCount != null) {
            //         var overflows = source.Where((_, index) => index + 1 >= maxCount);
            //         closes = closes.Merge(overflows);
            //     }
            //     return p.Window(() => closes).SelectMany(window => window.ToList()).Delay(TimeSpan.Zero);
            // });
            //
            // return publish;
        }
        // => source.Quiescent(delay,scheduler).SelectMany(list => resultSelector(list.ToNowObservable()));
        
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
        
        public static IObservable<TSource[]> BufferUntilCompleted<TSource>(this IObservable<TSource> source,bool skipEmpty=false) 
            => source.Buffer(Observable.Never<Unit>()).Where(sources => !skipEmpty || sources.Any()).Select(list => list.ToArray());

        /// <summary>
        /// Returns a connectable observable, that once connected, will start buffering data until the observer subscribes, at which time it will send all buffered data to the observer and then start sending new data.
        /// Thus, the observer may subscribe late to a hot observable yet still see all the data.  Later observers will not see the buffered events.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IConnectableObservable<T> BufferUntilSubscribed<T>(this IObservable<T> source) => new BufferUntilSubscribedObservable<T>(source, ImmediateScheduler);

        class BufferUntilSubscribedObservable<T>(IObservable<T> source, IScheduler scheduler)
            : IConnectableObservable<T> {
            private readonly Subject<T> _liveEvents = new();
            private bool _observationsStarted;
            private Queue<T> _buffer = new();
            private readonly object _gate = new();

            public IDisposable Subscribe(IObserver<T> observer) {
                lock (_gate) {
                    if (_observationsStarted) {
                        return _liveEvents.Subscribe(observer);
                    }

                    _observationsStarted = true;

                    var bufferedEvents =
                        GetBuffers().Concat()
                            .Finally(
                                RemoveBuffer); // Finally clause to remove the buffer if the first observer stops listening.
                    return _liveEvents.Merge(bufferedEvents).Subscribe(observer);
                }
            }

            public IDisposable Connect() => source.Subscribe(OnNext, _liveEvents.OnError, _liveEvents.OnCompleted);

            private void RemoveBuffer() {
                lock (_gate) {
                    _buffer = null;
                }
            }

            /// <summary>
            /// Acquires a lock and checks the buffer.  If it is empty, then replaces it with null and returns null.  Else replaces it with an empty buffer and returns the old buffer.
            /// </summary>
            /// <returns></returns>
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

            /// <summary>
            /// An enumerable of buffers that will complete when a call to GetAndReplaceBuffer() returns a null, e.g. when the observer has caught up with the incoming source data.
            /// </summary>
            /// <returns></returns>
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
        
        /// <summary>
        /// Emits a list every interval that contains all the currently buffered elements.
        /// </summary>
        public static IObservable<IList<TSource>> BufferHistorical<TSource>(this IObservable<TSource> source, TimeSpan interval, TimeSpan replayDuration) 
            => source.Replay(replayed => Observable.Interval(interval)
                .SelectMany(_ => replayed.TakeUntil(Observable.Return(Unit.Default, Scheduler.CurrentThread)).ToList())
                .TakeUntil(replayed.LastOrDefaultAsync()), replayDuration, ImmediateScheduler);
        
        public static IObservable<IList<TSource>> BufferOmitEmpty<TSource>(this IObservable<TSource> observable, TimeSpan maxDelay, int maxBufferCount) 
            => observable.GroupByUntil(_ => 1, g => Observable.Timer(maxDelay).Merge(g.Skip(maxBufferCount - 1).Take(1).Select(_ => 1L)))
                .Select(x => x.ToArray())
                .Switch();
        
        /// <summary>
        /// Splits the elements of a sequence into chunks that are starting with
        /// elements that satisfy the predicate.
        /// </summary>
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