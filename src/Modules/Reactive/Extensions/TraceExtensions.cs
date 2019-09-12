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
using DevExpress.Utils.Extensions;
using Fasterflect;
using Xpand.Source.Extensions.System.Exception;

namespace Xpand.XAF.Modules.Reactive.Extensions{
    public enum ObservableTraceStrategy{
        None,
        Default,
        All
    }

    public static class TraceExtensions{
        public static ConcurrentDictionary<Type, Func<object, string>> DefaultMembers{ get; }

        static TraceExtensions(){
            DefaultMembers=new ConcurrentDictionary<Type, Func<object,string>>();
            string FrameViewId(object o){
                var frame = o.CastTo<Frame>();
                return $"{o.GetType().FullName} - ctx: {frame.Context} - id: {frame.View?.Id}";
            }

            DefaultMembers.TryAdd(typeof(Frame), FrameViewId);
            DefaultMembers.TryAdd(typeof(Window), FrameViewId);
            string CollectionSource(object o) => $"{o.GetType().Name} - {((CollectionSourceBase) o).ObjectTypeInfo.FullName}";
            DefaultMembers.TryAdd(typeof(CollectionSource), CollectionSource);
            DefaultMembers.TryAdd(typeof(ShowViewParameters), o => {
                var showViewParameters = ((ShowViewParameters) o);
                return $"{nameof(ShowViewParameters)} - {showViewParameters.CreatedView.Id} - {showViewParameters.Context}";
            });
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
            if (DefaultMembers.TryGetValue(objectType, out var func)){
                v = func(v);
            }
            else if (v is IModelNode){
                v = $"{objectType.Name} - {((ModelNode) v).Id}";
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
                    var id = traceSource.Name.GetHashCode();
                    traceSource.TraceEvent(TraceEventType.Information, id,s);
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