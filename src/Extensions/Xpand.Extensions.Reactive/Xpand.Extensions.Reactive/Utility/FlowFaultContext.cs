using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> FlowFaultContext<T>(this IObservable<T> source, params IAsyncLocal[] asyncLocals)
            => Observable.Create<T>(observer => {
                var capturedValues = asyncLocals.Select(l => l.Value).ToArray();

                return source.Subscribe(
                    onNext: value => {
                        var originalValues = asyncLocals.Select(l => l.Value).ToArray();
                        try {
                            for (int i = 0; i < asyncLocals.Length; i++) {
                                asyncLocals[i].Value = capturedValues[i];
                            }

                            observer.OnNext(value);
                        }
                        finally {
                            for (int i = 0; i < asyncLocals.Length; i++) {
                                asyncLocals[i].Value = originalValues[i];
                            }
                        }
                    },
                    onError: error => {
                        var originalValues = asyncLocals.Select(l => l.Value).ToArray();
                        try {
                            for (int i = 0; i < asyncLocals.Length; i++) {
                                asyncLocals[i].Value = capturedValues[i];
                            }

                            observer.OnError(error);
                        }
                        finally {
                            for (int i = 0; i < asyncLocals.Length; i++) {
                                asyncLocals[i].Value = originalValues[i];
                            }
                        }
                    },
                    onCompleted: () => {
                        var originalValues = asyncLocals.Select(l => l.Value).ToArray();
                        try {
                            for (int i = 0; i < asyncLocals.Length; i++) {
                                asyncLocals[i].Value = capturedValues[i];
                            }

                            observer.OnCompleted();
                        }
                        finally {
                            for (int i = 0; i < asyncLocals.Length; i++) {
                                asyncLocals[i].Value = originalValues[i];
                            }
                        }
                    });
            });
        
        public static IAsyncLocal Wrap<T>(this AsyncLocal<T> asyncLocal) 
            => new AsyncLocalWrapper<T>(asyncLocal);        
    }

    public interface IAsyncLocal {
        object Value { get; set; }
    }

    internal class AsyncLocalWrapper<T>(AsyncLocal<T> asyncLocal) : IAsyncLocal {
        public object Value {
            get => asyncLocal.Value;
            set => asyncLocal.Value = (T)value;
        }
    }
}