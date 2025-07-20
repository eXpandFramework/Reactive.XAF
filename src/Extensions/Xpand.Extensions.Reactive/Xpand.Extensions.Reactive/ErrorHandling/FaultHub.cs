using System;
using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xpand.Extensions.DictionaryExtensions;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Reactive.ErrorHandling{
    public static class FaultHub {
        private static readonly AsyncLocal<bool> IsRetrying = new();
        static readonly AsyncLocal<Guid?> Ctx = new();
        static readonly Subject<Exception> PreRaw  = new();
        static readonly Subject<Exception> MainRaw = new();
        public static readonly ConcurrentDictionary<Guid, byte> Seen = new();
        public static readonly ISubject<Exception> PreBus  = Subject.Synchronize(PreRaw);
        public static readonly ISubject<Exception> Bus     = Subject.Synchronize(MainRaw);
        const string KeyCId     = "CorrelationId";
        public const string SkipKey = "FaultHub.Skip";
        const string PublishedKey  = "FaultHub.Published";
        public static bool IsSkipped(this Exception exception) => exception.Data.Contains(SkipKey);
        public static bool IsPublished(this Exception exception) => exception.Data.Contains(PublishedKey);
        
        public static IObservable<T> ResurrectFault<T>(this IObservable<T> source) 
            => source.Publish(sourceBus => Observable.Create<T>(observer => {
                var busSub = PreBus
                    .TakeUntil(sourceBus.WhenAlive())
                    .Take(1)                       
                    .Subscribe(ex => {
                        ex.MuteForBus();
                        observer.OnError(ex);
                    });
                var srcSub = sourceBus.Subscribe(observer);
                return new CompositeDisposable(busSub, srcSub,Disposable.Create(observer.OnCompleted));
            }));

        public static void MuteForBus(this Exception ex) => ex.Data[SkipKey] = true;
        public static IObservable<T> PropagateFaults<T>(this IObservable<T> source,
            Func<Exception,bool> match = null)
            => Observable.Create<T>(observer => {
                var inner = source.Subscribe(observer);                       
                var faults = Bus.Where(ex => match?.Invoke(ex) ?? true)      
                    .TakeUntil(source.WhenAlive())
                    .Subscribe(observer.OnError);
                return new CompositeDisposable(inner, faults);
            });
        [Obsolete("use ToResilinet")]
        public static IObservable<T> UseFaultHub<T>(this IObservable<T> source, Action<Exception> onError = null,Func<Exception, bool> match = null)
            => source.CompleteOnError(exception => {
                onError?.Invoke(exception);
                exception.Publish();
            }, e => match?.Invoke(e)??false);

        public static Guid? CorrelationId(this Exception ex) => (Guid?)ex.Data[KeyCId];

        public static Exception TagCorrelation(this Exception ex, Guid? correlationId = null) {
            lock (ex.Data.SyncRoot) {
                if (ex.Data[KeyCId] is Guid) return ex;
                ex.Data[KeyCId] = correlationId;
                return ex;
            }
        }
        
        public static bool Publish(this Exception ex) {
            Guid id;
            lock (ex.Data.SyncRoot) {
                if (ex.Data.Contains(PublishedKey)) return true;
                ex.Data[PublishedKey] = new object();
                if (!ex.Data.Contains(KeyCId) && Ctx.Value.HasValue)
                    ex.Data[KeyCId] = Ctx.Value;
                if (ex.Data.Contains(SkipKey)) return false;
                ex.TagOrigin();
                id = ex.Data[KeyCId] as Guid? ?? Guid.Empty;
            }
            if (id != Guid.Empty && !Seen.AddWithTtlAndCap(id)) return false;
            PreRaw.OnNext(ex);
            MainRaw.OnNext(ex);
            return true;
        }
        
        public static IObservable<T> Publish<T>(this Exception ex) 
            => ex.Publish() ? Observable.Empty<T>() : ex.Throw<T>();
        
        static IObservable<T> MakeResilient<T>(this IObservable<T> source,
            Func<IObservable<T>, IObservable<T>> retrySelector = null,[CallerMemberName]string caller="") {
            caller = caller;
            var isNested = IsRetrying.Value;

            var sourceWithRetry = source;
            if (retrySelector != null) {
                sourceWithRetry = Observable.Defer(() => {
                    IsRetrying.Value = true;
                    return retrySelector(source);
                }).Finally(() => {
                    IsRetrying.Value = isNested;
                });
            }
    
            return sourceWithRetry.Catch<T, Exception>(ex => {
                if (isNested) {
                    // We are a nested resilient boundary. Re-throw the exception
                    // to allow the outer retry strategy to handle it.
                    return Observable.Throw<T>(ex);
                }
                // We are the outermost boundary. Publish the final error and complete.
                return ex.Publish<T>();
            });
        }
        
        public static Func<TSource, IObservable<TResult>> ToResilient<TSource, TResult>(this Func<TSource, IObservable<TResult>> selector,
            Func<IObservable<TResult>, IObservable<TResult>> retrySelector = null)
            => x => selector.Defer(() => selector(x),retrySelector);

        public static Func<TSource, IObservable<TResult>> ToResilient<TSource, TResult>(this Func<TSource, Task<TResult>> selector,
            Func<IObservable<TResult>, IObservable<TResult>> retrySelector = null)
            => x => selector.Defer(() => selector(x).ToObservable(),retrySelector);

        public static Func<T1, int, IObservable<T2>> ToResilient<T1, T2>(this Func<T1, int, IObservable<T2>> selector,
            Func<IObservable<T2>, IObservable<T2>> retrySelector = null)
            => (x, i) => selector.Defer(() => selector(x, i),retrySelector:retrySelector);

        public static Func<IObservable<TResult>> ToResilient<TResult>(this Func<IObservable<TResult>> action,
            Func<IObservable<TResult>, IObservable<TResult>> retrySelector = null)
            => () => Observable.Defer(action).MakeResilient(retrySelector);

        public static IObservable<TResult> ToResilient<TResult>(this IObservable<TResult> source,
            Func<IObservable<TResult>, IObservable<TResult>> retrySelector = null)
            => source.MakeResilient(retrySelector);
    }
}