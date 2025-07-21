using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xpand.Extensions.DictionaryExtensions;
using Xpand.Extensions.ExceptionExtensions;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Reactive.ErrorHandling{
    public static class FaultHub {
        private static readonly AsyncLocal<bool> IsRetrying = new();
        internal static readonly AsyncLocal<List<Func<Exception, bool>>> HandlersContext = new();
        static readonly AsyncLocal<Guid?> Ctx = new();
        static readonly Subject<Exception> PreRaw  = new();
        static readonly Subject<Exception> MainRaw = new();
        public static readonly ConcurrentDictionary<string, byte> Seen = new();
        public static readonly ISubject<Exception> PreBus  = Subject.Synchronize(PreRaw);
        public static readonly ISubject<Exception> Bus     = Subject.Synchronize(MainRaw);
        const string KeyCId     = "CorrelationId";
        public const string SkipKey = "FaultHub.Skip";
        const string PublishedKey  = "FaultHub.Published";
        public static bool IsSkipped(this Exception exception) => exception.AccessData(data => data.Contains(SkipKey));
        public static bool IsPublished(this Exception exception) => exception.AccessData(data => data.Contains(PublishedKey));
        
        public static IObservable<T> WithSharedFaultContext<T>(this IObservable<T> source) {
            if (Ctx.Value.HasValue) {
                return source;
            }
            return Observable.Defer(() => {
                Ctx.Value = Guid.NewGuid();
                return source.Finally(() => Ctx.Value = null);
            });
        }
        public static IObservable<T> WithNewFaultContext<T>(this IObservable<T> source) {
            return Observable.Defer(() => {
                var originalCtx = Ctx.Value;
                Ctx.Value = Guid.NewGuid();
                return source.Finally(() => Ctx.Value = originalCtx);
            });
        }
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

        public static void MuteForBus(this Exception ex) => ex.AccessData(data => data[SkipKey]=true);
        public static IObservable<T> PropagateFaults<T>(this IObservable<T> source,
            Func<Exception,bool> match = null)
            => Observable.Create<T>(observer => {
                var inner = source.Subscribe(observer);                       
                var faults = Bus.Where(ex => match?.Invoke(ex) ?? true)      
                    .TakeUntil(source.WhenAlive())
                    .Subscribe(observer.OnError);
                return new CompositeDisposable(inner, faults);
            });
        
        public static IObservable<T> UseFaultHub<T>(this IObservable<T> source)
            => source.ToResilient();

        public static Guid? CorrelationId(this Exception ex) => (Guid?)ex.AccessData(data => data[KeyCId]);

        public static Exception TagCorrelation(this Exception ex, Guid? correlationId = null) 
            => ex.AccessData(data => {
                if (data[KeyCId] is Guid) return ex;
                data[KeyCId] = correlationId;
                return ex;
            });

        private enum PublishAction { Continue, StopAndReturnTrue, StopAndReturnFalse }
        public static bool Publish(this Exception ex, [CallerMemberName] string caller = "") {
            var (action, correlationId) = ex.AccessData(data => {
                if (data.Contains(PublishedKey)) {
                    return (PublishAction.StopAndReturnTrue, Guid.Empty);
                }
                data[PublishedKey] = new object();
                if (!data.Contains(KeyCId) && Ctx.Value.HasValue) {
                    data[KeyCId] = Ctx.Value;
                }
                if (data.Contains(SkipKey)) {
                    return (PublishAction.StopAndReturnFalse, Guid.Empty);
                }
                ex.TagOrigin(); 
                var id = data[KeyCId] as Guid? ?? Guid.Empty;
                return (PublishAction.Continue, id);
            });

            switch (action) {
                case PublishAction.StopAndReturnTrue:
                    return true;
                case PublishAction.StopAndReturnFalse:
                    return false;
            }

            var deduplicationKey = $"{correlationId}:{ex.GetType().FullName}:{ex.Message}";
            if (correlationId != Guid.Empty && !Seen.AddWithTtlAndCap(deduplicationKey)) {
                return false;
            }
        
            PreRaw.OnNext(ex);
            MainRaw.OnNext(ex);
            return true;
        }
        
        public static IObservable<T> Publish<T>(this Exception ex,[CallerMemberName]string caller="") {
            return ex.Publish() ? Observable.Empty<T>() : Observable.Throw<T>(ex);
        }
        
        static IObservable<T> MakeResilient<T>(this IObservable<T> source,
            Func<IObservable<T>, IObservable<T>> retrySelector = null)
            => Observable.Defer(() => {
                var isNestedRetry = IsRetrying.Value;
                var streamToCatch = source;
                if (retrySelector != null) {
                    streamToCatch = Observable.Defer(() => {
                        IsRetrying.Value = true;
                        return retrySelector(source);
                    }).Finally(() => IsRetrying.Value = isNestedRetry);
                }
                return streamToCatch.Catch<T, Exception>(ex => {
                    var handlers = HandlersContext.Value;
                    return handlers != null && handlers.Any(handler => handler(ex)) ? Observable.Throw<T>(ex) :
                        isNestedRetry ? Observable.Throw<T>(ex) : ex.Publish<T>();
                });
            });

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
            Func<IObservable<TResult>, IObservable<TResult>> retrySelector = null,[CallerMemberName]string caller="")
            => () => Observable.Defer(action).MakeResilient(retrySelector);

        public static IObservable<TResult> ToResilient<TResult>(this IObservable<TResult> source,
            Func<IObservable<TResult>, IObservable<TResult>> retrySelector = null,[CallerMemberName]string caller="")
            => source.MakeResilient(retrySelector);
        
    }
}