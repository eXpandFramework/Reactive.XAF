using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace Xpand.Extensions.Reactive.ErrorHandling{
    public static class FaultHub {
        static readonly Subject<Exception> Inner = new();
        
        public static readonly ISubject<Exception> Bus = Subject.Synchronize(Inner);

        public static IObservable<T> UseFaultHub<T>(this IObservable<T> source, Action<Exception> onError = null,Func<Exception, bool> match = null)
            => source.CompleteOnError(exception => {
                onError?.Invoke(exception);
                exception.Publish();
            }, e => match?.Invoke(e)??false);

        public static bool Publish(this Exception ex) {
            if (!Inner.HasObservers) return false;
            Bus.OnNext(ex.Tag());
            return true;
        }

        public static IObservable<T> Publish<T>(this Exception ex) 
            => ex.Publish()? Observable.Empty<T>():Observable.Throw<T>(ex);

        public static Func<TSource, IObservable<TResult>> WithResilience<TSource, TResult>(this Func<TSource, Task<TResult>> selector)
            => x => {
                IObservable<TResult> inner;
                try {
                    inner = selector(x).ToObservable();
                }
                catch (Exception ex) {
                    return ex.Tag().Publish<TResult>();   
                }
                return inner.Catch<TResult, Exception>(ex => ex.Tag().Publish<TResult>());
            };
        
        public static Func<TSource,IObservable<TResult>> WithResilience<TSource,TResult>(
            this Func<TSource,IObservable<TResult>> selector)
            => x => {
                IObservable<TResult> inner;
                try{
                    inner = selector(x);
                }
                catch (Exception ex){
                    return ex.Tag().Publish<TResult>();
                }

                return inner.Catch<TResult,Exception>(ex => ex.Tag().Publish<TResult>());
            };
        
        public static Func<T1, int, IObservable<T2>> WithResilience<T1, T2>(this Func<T1, int, IObservable<T2>> selector)
            => (x, i) => {
                IObservable<T2> inner;
                try {
                    inner = selector(x, i);
                }
                catch (Exception ex) {
                    return ex.Tag().Publish<T2>();
                }
                return inner.Catch<T2, Exception>(ex => ex.Tag().Publish<T2>());
            };

        public static Func<IObservable<TResult>> WithResilience<TResult>(this Func<IObservable<TResult>> action)
            => () => {
                IObservable<TResult> inner;
                try {
                    inner = action();
                }
                catch (Exception ex) {
                    return ex.Tag().Publish<TResult>();
                }

                return inner.Catch<TResult, Exception>(ex => ex.Tag().Publish<TResult>());
            };
    }
}