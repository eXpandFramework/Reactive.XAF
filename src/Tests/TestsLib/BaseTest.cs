using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using Xunit.Abstractions;
using IDisposable = System.IDisposable;

namespace TestsLib{
    public static class MyRxExtensions{
        

        public static IObservable<T> RetryWithBackoff<T>(this IObservable<T> source,int retryCount = 3,
            Func<int, TimeSpan> strategy = null,Func<Exception, bool> retryOnError = null,IScheduler scheduler = null){
            strategy = strategy ?? (n =>TimeSpan.FromSeconds(Math.Pow(n, 2))) ;
            var attempt = 0;
            retryOnError = retryOnError ?? (_ => true);
            return Observable.Defer(() => (++attempt == 1 ? source : source.DelaySubscription(strategy(attempt - 1), scheduler))
                    .Select(item => (true, item, (Exception)null))
                    .Catch<(bool, T, Exception), Exception>(e =>retryOnError(e)? Observable.Throw<(bool, T, Exception)>(e)
                        : Observable.Return<(bool, T, Exception)>((false, default, e))))
                .Retry(retryCount)
                .SelectMany(t => t.Item1
                    ? Observable.Return(t.Item2)
                    : Observable.Throw<T>(t.Item3));
        }

        public static IObservable<T> DelaySubscription<T>(this IObservable<T> source,
            TimeSpan delay, IScheduler scheduler = null){
            if (scheduler == null) return Observable.Timer(delay).SelectMany(_ => source);
            return Observable.Timer(delay, scheduler).SelectMany(_ => source);
        }
    }

    public abstract class BaseTest : IDisposable{

        protected async Task Execute(Action action){
            await Observable.Defer(() => Observable.Start(action)).RetryWithBackoff(3,retryOnError:exception => true).FirstAsync();
        }
        public const string NotImplemented = "NotImplemented";
        protected BaseTest(ITestOutputHelper output){
            Output = output;
        }

        protected BaseTest(){
        }

        public ITestOutputHelper Output{ get; }

        

        public virtual void Dispose(){
            XpoTypesInfoHelper.Reset();
            XafTypesInfo.HardReset();
        }
    }
}