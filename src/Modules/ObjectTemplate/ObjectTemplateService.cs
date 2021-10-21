using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using RazorLight;
using RazorLight.Compilation;
using RazorLight.Generation;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.XAF.Modules.ObjectTemplate.Template;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.ObjectTemplate{
    public static class ObjectTemplateService {
        
        internal static IObservable<TSource> TraceObjectTemplate<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, ObjectTemplateModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);
        
        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager)
            => manager.WhenApplication(application => application.WhenSetupComplete()
                .SelectMany(_ => application.CacheRules())).ToUnit();

        private static IObservable<(IObjectSpace objectSpace, T instance, ObjectModification modification)> WhenObjectExistsOrModified<T>(this XafApplication application)
            => Observable.Using(application.CreateObjectSpace, space
                => space.GetObjectsQuery<T>().ToNowObservable().Select(o => (space, o, default(ObjectModification)))
                    .Merge(application.WhenCommitedDetailed<T>()
                        .SelectMany(t => t.details.Select(t1 => (t.objectSpace,t1.instance,t1.modification)))));

        private static IObservable<Unit> CacheRules(this XafApplication application)
            => throw new NotImplementedException("use line below");
            // => application.WhenObjectExistsOrModified<BusinessObjects.ObjectTemplate>().SelectMany(t => t.Build());
            // => application.ObserveType<ObjectTemplate.BusinessObjects.ObjectTemplate>(template => template.Build(),
                // t => t.instance.Build(t.Modification == ObjectModification.Deleted));

        private static IObservable<Unit> Build(this (IObjectSpace objectSpace, BusinessObjects.ObjectTemplate instance, ObjectModification? modification) template, bool remove=false) {
            var engine = new RazorLightEngineBuilder()
                .UseProject(new ObjectTemplateRazorProject())
                .UseMemoryCachingProvider()
                .Build();
            return engine.CompileRenderAsync(template.instance.Oid.ToString(),new object()).ToObservable().ToUnit();
        }
        
        

    }
}