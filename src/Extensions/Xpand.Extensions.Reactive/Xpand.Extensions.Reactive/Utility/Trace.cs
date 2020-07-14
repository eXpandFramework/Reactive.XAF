using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.ExceptionExtensions;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.Reactive.Utility{
    public enum ObservableTraceStrategy{
        None,
        Default,
        All
    }

    public static partial class Utility{
        private static ConcurrentDictionary<Type, Func<object, string>> Serialization{ get; }
        

        public static Func<object, string> Serializer = o => o.ToString();
        static Utility() => Serialization=new ConcurrentDictionary<Type, Func<object,string>>();

        public static bool AddTraceSerialization(Type type) => Serialization.TryAdd(type, Serializer);

        public static bool AddTraceSerialization<T>(Func<T,string> function) => Serialization.TryAdd(typeof(T), o => function((T) o));


        public static IObservable<TSource> Trace<TSource>(this IObservable<TSource> source, string name = null,TraceSource traceSource=null,
            Func<TSource,string> messageFactory=null,Func<Exception,string> errorMessageFactory=null, Action<string> traceAction = null, 
            ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0) => Observable.Create<TSource>(observer => {
                void Action(string m, object v, Action<string> ta){
                    if (traceSource?.Switch.Level == SourceLevels.Off){
                        return;
                    }

                    string value = null;
                    if (v!=null){
                        value = $@"{CalculateValue(v, o => messageFactory.GetMessageValue( errorMessageFactory, o))}";
                    }

                    var mName = memberName;
                    if (m == "OnNext"){
                        mName = $"{memberName} =>{GetSourceName<TSource>()}";
                    }

                    var message = $"{name}.{Path.GetFileNameWithoutExtension(sourceFilePath)}.{mName}({sourceLineNumber}): {m}({value})".TrimStart('.');
                    ta(message);
                }

                if (traceStrategy == ObservableTraceStrategy.All)
                    Action("Subscribe", "", traceAction.TraceInformation(traceSource));
                var disposable = source.Subscribe(
                    v => {
                        if (traceStrategy != ObservableTraceStrategy.None){
                            Action("OnNext", v, traceAction.TraceInformation(traceSource));
                        }
                        observer.OnNext(v);
                    },
                    e => {
                        Action("OnError", e.GetAllInfo(), traceAction.TraceError(traceSource));
                        observer.OnError(e);
                    },
                    () => {
                        if (traceStrategy == ObservableTraceStrategy.All)
                            Action("OnCompleted", "", traceAction.TraceInformation(traceSource));
                        observer.OnCompleted();
                    });
                return () => {
                    if (traceStrategy == ObservableTraceStrategy.All)
                        Action("Dispose", "", traceAction.TraceInformation(traceSource));
                    disposable.Dispose();
                };
            });

        private static string GetMessageValue<TSource>(this Func<TSource, string> messageFactory, Func<Exception, string> errorMessageFactory, object o){
            try{
                return o switch{
                    TSource t => (messageFactory?.Invoke(t) ?? o).ToString(),
                    Exception e => (errorMessageFactory?.Invoke(e) ?? o).ToString(),
                    _ => o.ToString()
                };
            }
            catch (Exception e){
                return e.Message;
            }
        }

        private static string GetSourceName<TSource>() => 
	        typeof(TSource).IsGenericType ? string.Join(",", typeof(TSource).GetGenericArguments().Select(type => type.Name)) : typeof(TSource).Name;

        private static object CalculateValue(object v, Func<object, string> messageFactory) =>
            messageFactory != null ? messageFactory(v) : v.GetType().FromHierarchy(_ => _.BaseType)
                .Select(_ => Serialization.TryGetValue(_, out var func) ? func(v) : null).FirstOrDefault()?? v;

        private static Action<string> TraceError(this Action<string> traceAction, TraceSource traceSource) =>
	        traceAction ?? (s => {
		        if (traceSource != null){
			        traceSource.TraceEvent(TraceEventType.Error, 0,s);
		        }
		        else{
			        System.Diagnostics.Trace.TraceError(s);
		        }
	        });


        private static Action<string> TraceInformation(this Action<string> traceAction, TraceSource traceSource) =>
	        traceAction ?? (s => {
		        if (traceSource != null){
			        traceSource.TraceEvent(TraceEventType.Information, 0,s);
		        }
		        else{
			        System.Diagnostics.Trace.TraceInformation(s);
		        }

	        });
    }

}