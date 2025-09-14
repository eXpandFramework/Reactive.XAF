using System;
using System.Collections;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<TSource> UseContext<TSource>(this IObservable<TSource> source, object contextValue, params IAsyncLocal[] contexts) 
            => Observable.Using(() => {
                    var originalValues = contexts
                        .Do(local => local.Value=contextValue)
                        .Select(c => c.Value).ToArray();
                    return Disposable.Create(contexts, locals => {
                        for (int i = 0; i < locals.Length; i++) {
                            locals[i].Value = originalValues[i];
                        }
                    });
                },
                _ => source);

        public static IObservable<T> FlowContext<T>(this IObservable<T> source,Func<IObservable<T>,IObservable<T>> retrySelector=null, params IAsyncLocal[] context)
            => Observable.Create<T>(observer => {
                var capturedValues = context.Select(l => l.Value).ToArray();
                return (retrySelector?.Invoke(source.ErrorBus( context))??source)
                    .Subscribe(onNext: value => context.FlowContext(() => observer.OnNext(value),capturedValues),
                        onError: error => context.FlowContext(() => observer.OnError(error),capturedValues,error.Data),
                    onCompleted: () => context.FlowContext(observer.OnCompleted,capturedValues));
            });

        private static IObservable<T> ErrorBus<T>(this IObservable<T> source, IAsyncLocal[] context) 
            => source.Catch((Exception ex) => {
                var objects = context.Select(l => l.Value).ToArray();
                ex.Data[nameof(FlowContext)] = objects;
                return Observable.Throw<T>(ex);
            });

        private static void FlowContext(this IAsyncLocal[] context,Action observerAction, object[] capturedValues,IDictionary errorData=null){
            var originalValues = context.Select(l => l.Value).ToArray();
            var hasFlowContextData = errorData?.Contains(nameof(FlowContext))??false;
            try {
                capturedValues=(object[])(hasFlowContextData?errorData[nameof(FlowContext)]:capturedValues);
                for (int i = 0; i < context.Length; i++) {
                    context[i].Value = capturedValues![i];
                }
                observerAction();
            }
            finally {
                if (!hasFlowContextData) {
                    for (int i = 0; i < context.Length; i++) {
                        context[i].Value = originalValues[i];
                    }    
                }
            }        
        }

        public static IAsyncLocal Wrap<T>(this AsyncLocal<T> asyncLocal) 
            => new AsyncLocalWrapper<T>(asyncLocal);        
    }

    public interface IAsyncLocal {
        object Value { get; set; }
    }

    internal class AsyncLocalWrapper<T>(AsyncLocal<T> asyncLocal) : IAsyncLocal {
        public object Value {
            get => asyncLocal.Value;
            set => asyncLocal.Value = value is T val ? val : default;
        }
    }
}