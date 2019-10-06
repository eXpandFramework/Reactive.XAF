using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using Fasterflect;
using Xpand.Source.Extensions.Linq;
using Xpand.Source.Extensions.System.Exception;

namespace Xpand.XAF.Modules.Reactive.Extensions{
    public enum ObservableTraceStrategy{
        None,
        Default,
        All
    }

    public static class TraceExtensions{
        private static ConcurrentDictionary<Type, Func<object, string>> Serialization{ get; }

        static TraceExtensions(){
            Serialization=new ConcurrentDictionary<Type, Func<object,string>>();
            AddSerialization<Frame>(_ => $"{_.GetType().FullName} - ctx: {_.Context} - id: {_.View?.Id}");
            AddSerialization<CollectionSourceBase>(_ => $"{_.GetType().Name} - {_.ObjectTypeInfo.FullName}");
            AddSerialization<ShowViewParameters>(_ => $"{nameof(ShowViewParameters)} - {_.CreatedView.Id} - {_.Context}");
            AddSerialization<ModuleBase>(_ => _.Name);
        }

        public static bool AddSerialization<T>(Func<T,string> function){
            return Serialization.TryAdd(typeof(T), o => function((T) o));
        }

        internal static IObservable<TSource> TraceRX<TSource>(this IObservable<TSource> source, string name = null,
            Action<string> traceAction = null,
            ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0){
            return source.Trace(name, ReactiveModule.TraceSource, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);
        }

        public static IObservable<TSource> Trace<TSource>(this IObservable<TSource> source, string name = null,TraceSource traceSource=null,
            Action<string> traceAction = null, 
            ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0){
            if (traceSource?.Switch.Level == SourceLevels.Off){
                return source;
            }

            return Observable.Create<TSource>(observer => {
                void Action(string m, object v, Action<string> ta){
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
            var serialization = objectType.FromHierarchy(_ => _.BaseType)
                .FirstOrDefault(_ => {
                    if (Serialization.TryGetValue(_, out var func)){
                        v = func(v);
                        return true;
                    }

                    return false;
                });
            if (serialization==null){
                if (v is IModelNode){
                    v = $"{objectType.Name} - {((ModelNode) v).Id}";
                }
            }

            var attributes = objectType.Attributes();
            var defaultPropertyAttribute = attributes.OfType<XafDefaultPropertyAttribute>().FirstOrDefault();
            if (defaultPropertyAttribute != null){
                v = $"{objectType.Name} - {v.GetPropertyValue(defaultPropertyAttribute.Name)}";
            }

            var xafDefaultPropertyAttribute = objectType.GetInterfaces().SelectMany(type => type.Attributes())
                .OfType<XafDefaultPropertyAttribute>().FirstOrDefault();
            if (xafDefaultPropertyAttribute != null){
                v = $"{objectType.Name} - {v.GetPropertyValue(xafDefaultPropertyAttribute.Name)}";
            }

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
    public class ReactiveTraceSource:TraceSource{
        public ReactiveTraceSource(string name) : base(name){
            Switch.Level=SourceLevels.Verbose;
        }

        public ReactiveTraceSource(string name, SourceLevels defaultLevel) : base(name, defaultLevel){
            Switch.Level=SourceLevels.Verbose;
        }
    }

}