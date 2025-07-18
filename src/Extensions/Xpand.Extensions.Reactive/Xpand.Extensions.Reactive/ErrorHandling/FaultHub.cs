using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Xpand.Extensions.DictionaryExtensions;

namespace Xpand.Extensions.Reactive.ErrorHandling{
    public static class FaultHub {
        static readonly Subject<Exception> Inner = new();
        
        public static readonly ISubject<Exception> Bus = Subject.Synchronize(Inner);
        
        public static IObservable<T> UseFaultHub<T>(this IObservable<T> source, Action<Exception> onError = null,Func<Exception, bool> match = null)
            => source.CompleteOnError(exception => {
                onError?.Invoke(exception);
                exception.Publish();
            }, e => match?.Invoke(e)??false);

        

        const string KeyCId     = "CorrelationId";

        public static readonly ConcurrentDictionary<Guid, byte> Seen = new();

        public static Guid? CorrelationId(this Exception ex) => (Guid?)ex.Data[KeyCId];

        public static Exception TagCorrelation(this Exception ex,Guid? correlationId = null ) {
            if (ex.Data[KeyCId] is Guid) return ex;
            ex.Data[KeyCId] = correlationId;
            return ex;
        }
        public static bool Publish(this Exception ex) {
            if (!Inner.HasObservers) return false;

            var id = ex.Data[KeyCId] as Guid? ?? Guid.Empty;
            if (id != Guid.Empty) {
                Seen.AddWithTtlAndCap(id);
            }
            Bus.OnNext(ex.TagOrigin());
            return true;
        }
        public static IObservable<T> Publish<T>(this Exception ex) 
            => ex.Publish() ? Observable.Empty<T>() : Observable.Throw<T>(ex);

        public static Func<TSource, IObservable<TResult>> WithResilience<TSource, TResult>(this Func<TSource, Task<TResult>> selector,
            Func<IObservable<TResult>, IObservable<TResult>> retrySelector = null)
            => x => {
                var core = Observable.Defer(() => selector(x).ToObservable()).Catch<TResult, Exception>(e => e.TagOrigin().Publish<TResult>());
                return retrySelector != null ? retrySelector(core) : core;
            };
        
        public static Func<TSource, IObservable<TResult>> WithResilience<TSource, TResult>(this Func<TSource, IObservable<TResult>> selector,
            Func<IObservable<TResult>, IObservable<TResult>> retrySelector = null)
            => x => {
                var core = Observable.Defer(() => selector(x)).Catch<TResult, Exception>(ex => ex.TagOrigin().Publish<TResult>());
                return retrySelector != null ? retrySelector(core) : core;
            };
        
        public static Func<T1, int, IObservable<T2>> WithResilience<T1, T2>(this Func<T1, int, IObservable<T2>> selector,
            Func<IObservable<T2>, IObservable<T2>> retrySelector = null)
            => (x, i) => {
                var core = Observable.Defer(() => selector(x, i)).Catch<T2, Exception>(e => e.TagOrigin().Publish<T2>());
                return retrySelector != null ? retrySelector(core) : core;
            };

        public static Func<IObservable<TResult>> WithResilience<TResult>(this Func<IObservable<TResult>> action,
            Func<IObservable<TResult>, IObservable<TResult>> retrySelector = null)
            => () => {
                var core = Observable.Defer(action).Catch<TResult, Exception>(e => e.TagOrigin().Publish<TResult>());
                return retrySelector != null ? retrySelector(core) : core;
            };
        
        public static IObservable<TResult> WithResilient<TResult>(this IObservable<TResult> source,
            Func<IObservable<TResult>, IObservable<TResult>> retrySelector = null)
            => (retrySelector?.Invoke(source) ?? source).Catch<TResult, Exception>(ex => ex.TagOrigin().Publish<TResult>());
    }
}