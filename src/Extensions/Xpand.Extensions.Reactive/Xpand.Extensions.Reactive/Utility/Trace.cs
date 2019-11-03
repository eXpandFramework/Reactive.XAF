using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.Exception;
using Xpand.Extensions.Linq;

namespace Xpand.Extensions.Reactive.Utility{
    public enum ObservableTraceStrategy{
        None,
        Default,
        All
    }

    public static partial class Utility{
        private static ConcurrentDictionary<Type, Func<object, string>> Serialization{ get; }

        static Utility(){
            Serialization=new ConcurrentDictionary<Type, Func<object,string>>();
        }

        public static bool AddTraceSerialization<T>(Func<T,string> function){
            return Serialization.TryAdd(typeof(T), o => function((T) o));
        }


        public static IObservable<TSource> Trace<TSource>(this IObservable<TSource> source, string name = null,TraceSource traceSource=null,
            Action<string> traceAction = null, 
            ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0){
            

            return Observable.Create<TSource>(observer => {
                void Action(string m, object v, Action<string> ta){
                    if (traceSource?.Switch.Level == SourceLevels.Off){
                        return;
                    }
                    if (v!=null){
                        v = CalculateValue(v);
                    }
                    ta($"{name}.{Path.GetFileNameWithoutExtension(sourceFilePath)}.{memberName}({sourceLineNumber}): {m}({v})".TrimStart('.'));
                }

                if (traceStrategy == ObservableTraceStrategy.All)
                    Action("Subscribe", "", traceAction.TraceInformation(traceSource));
                var disposable = source.Subscribe(
                    v => {
                        if (traceStrategy != ObservableTraceStrategy.None)
                            Action("OnNext", v, traceAction.TraceInformation(traceSource));
                        observer.OnNext(v);
                    },
                    e => {
                        Action("OnError", e.GetAllMessages(), traceAction.TraceError(traceSource));
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
        }

        private static object CalculateValue(object v){
            var objectType = v.GetType();
            var serilialization = objectType.FromHierarchy(_ => _.BaseType)
                .Select(_ => Serialization.TryGetValue(_, out var func) ? func(v) : null)
                .WhereNotDefault().FirstOrDefault();
            if (serilialization != null){
                return serilialization;
            }
//            if (serialization==null){
//                if (v is IModelNode){
//                    v = $"{objectType.Name} - {((ModelNode) v).Id}";
//                }
//            }
//
//            var attributes = objectType.Attributes();
//            var defaultPropertyAttribute = attributes.OfType<XafDefaultPropertyAttribute>().FirstOrDefault();
//            if (defaultPropertyAttribute != null){
//                v = $"{objectType.Name} - {v.GetPropertyValue(defaultPropertyAttribute.Name)}";
//            }
//
//            var xafDefaultPropertyAttribute = objectType.GetInterfaces().SelectMany(type => type.Attributes())
//                .OfType<XafDefaultPropertyAttribute>().FirstOrDefault();
//            if (xafDefaultPropertyAttribute != null){
//                v = $"{objectType.Name} - {v.GetPropertyValue(xafDefaultPropertyAttribute.Name)}";
//            }

            return v;
        }

        private static Action<string> TraceError(this Action<string> traceAction, TraceSource traceSource){
            var action = traceAction;
            action = action ?? (s => {
                if (traceSource != null){
                    traceSource.TraceEvent(TraceEventType.Error, 0,s);
                }
                else{
                    System.Diagnostics.Trace.TraceError(s);
                }
            });
            return action;
        }

        
        private static Action<string> TraceInformation(this Action<string> traceAction, TraceSource traceSource){
            var action = traceAction;
            action = action ?? (s => {
                if (traceSource != null){
                    traceSource.TraceEvent(TraceEventType.Information, 0,s);
                }
                else{
                    System.Diagnostics.Trace.TraceInformation(s);
                }

            });
            return action;
        }
    }

}