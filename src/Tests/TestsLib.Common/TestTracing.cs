using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using DevExpress.Persistent.Base;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.TestsLib.Common{
    public class TestTracing : Tracing{
        readonly Subject<Exception> _errorSubject = new();
        static readonly Subject<GenericEventArgs<IObservable<Exception>>> CustomizeErrorSubject = new();

        public static IObservable<T> Handle<T>(Func<T,bool> match=null) where T : Exception
            => WhenCustomizeError().SelectMany(e => e.Instance.OfType<T>()
                .Where(exception => match?.Invoke(exception)??true)
                .Do(_ => e.SetInstance(_ => Observable.Empty<Exception>())));
        
        public static IObservable<GenericEventArgs<IObservable<Exception>>> WhenCustomizeError()
            => CustomizeErrorSubject.AsObservable();
        
        public static IObservable<Exception> WhenError([CallerMemberName]string caller="") 
            => ((TestTracing)Tracer)._errorSubject
                .SelectMany(exception => {
                    var e = new GenericEventArgs<IObservable<Exception>>(exception.Observe());
                    CustomizeErrorSubject.OnNext(e);
                    return e.Instance;
                })
                .Select(exception => exception.ToTestException(caller)).AsObservable();

        public static IObservable<Tracing> Use() 
            => typeof(Tracing).WhenEvent<CreateCustomTracerEventArgs>(nameof(CreateCustomTracer))
                .Select(e => e.Tracer = new TestTracing()).Take(1).Cast<Tracing>()
                .Merge(Unit.Default.DeferAction(_ => Initialize()).IgnoreElements().To<Tracing>());


        public override void LogVerboseError(Exception exception){
            base.LogVerboseError(exception);
            Notify(exception);
        }
            
        private void Notify(Exception exception) => _errorSubject.OnNext(exception);
        readonly Subject<Exception> _exceptions=new();

        public IObservable<Exception> Exceptions => _exceptions.AsObservable();

        public override void LogError(Exception exception){
            _exceptions.OnNext(exception);
            base.LogError(exception);
            Notify(exception);
        }
    }
    
    public static class TracingExtensions{
        public static IObservable<T> LogError<T>(this IObservable<T> source) 
            => source.Publish(obs => TestTracing.WhenError().ThrowTestException().To<T>().Merge(obs).TakeUntilCompleted(obs))
                .DoOnError(exception => Tracing.Tracer.LogError(exception));
    }
}