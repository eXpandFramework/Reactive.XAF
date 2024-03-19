using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ConcurrentCollections;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using Microsoft.CodeAnalysis;
using RazorLight;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.Tracing;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.RazorView.Template;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.RazorView{
    public static class RazorViewService {
        public static readonly ConcurrentHashSet<MetadataReference> MetadataReferences;

        static RazorViewService() 
            => MetadataReferences = new ConcurrentHashSet<MetadataReference>(Directory.GetFiles(AppDomain.CurrentDomain.ApplicationPath(), "*.dll").ToNowObservable()
                .Where(IsAssembly)
                .SelectMany(path => Observable.Start(() => MetadataReference.CreateFromFile(path))
                    .OnErrorResumeNext(Observable.Empty<MetadataReference>())).ToEnumerable());
        static bool IsAssembly(string path) {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var peReader = new PEReader(fs);
            try {
                return peReader.HasMetadata && peReader.GetMetadataReader().IsAssembly;
            }
            catch  {
                return false;
            }
        }
        internal static IObservable<TSource> TraceObjectTemplate<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<ITraceEvent> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,Func<string> allMessageFactory = null,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, RazorViewModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy,allMessageFactory, memberName,sourceFilePath,sourceLineNumber);
        
        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager)
            => manager.WhenApplication(application => application.WhenSetupComplete()
                    .SelectMany(_ => application.RenderPreview())).ToUnit()
                .Merge(ConfigureRichEditFormat(manager));

        private static IObservable<Unit> ConfigureRichEditFormat(this ApplicationModulesManager manager) 
            => manager.WhenGeneratingModelNodes<IModelBOModel>()
                .SelectMany(model => {
                    var modelClass = model.GetClass(typeof(RazorView.BusinessObjects.RazorView));
                    return new[] { nameof(BusinessObjects.RazorView.Preview), nameof(BusinessObjects.RazorView.Template) }.ToNowObservable()
                        .Select(member => modelClass.FindMember(member))
                        .Do(member => {
                            var modelNode = ((ModelNode)member);
                            var modelValueInfo = modelNode.GetValueInfo("DocumentStorageFormat");
                            if (modelValueInfo != null) {
                                modelNode.SetValue("DocumentStorageFormat", modelValueInfo.PropertyType,Enum.Parse(modelValueInfo.PropertyType,"Html"));   
                            }
                        });
                })
                .ToUnit();

        private static IObservable<Unit> RenderPreview(this XafApplication application) 
            => application.WhenFrame()
                .WhenFrame(typeof(RazorView.BusinessObjects.RazorView)).WhenFrame(ViewType.DetailView)
                .SelectMany(frame => {
                    var objectTemplate = ((RazorView.BusinessObjects.RazorView)frame.View.CurrentObject);
                    return objectTemplate.Render().ToObservable().Do(s => objectTemplate.Preview=s).ToUnit()
                        .Concat(frame.View.ObjectSpace.MonitorObjectTemplateChange().RenderPreview().ToUnit());
                });

        private static IObservable<RazorView.BusinessObjects.RazorView> MonitorObjectTemplateChange(this IObjectSpace objectSpace)
            => objectSpace.WhenObjectChanged().Where(t 
                    => new[] {
                            nameof(BusinessObjects.RazorView.Template), nameof(BusinessObjects.RazorView.ModelType),
                            nameof(BusinessObjects.RazorView.ModelCriteria)
                        }
                        .Contains(t.e.PropertyName)&&t.e.Object is BusinessObjects.RazorView)
                .Select(t => t.e.Object).Cast<RazorView.BusinessObjects.RazorView>();
        
        static IObservable<RazorView.BusinessObjects.RazorView> RenderPreview(this IObservable<RazorView.BusinessObjects.RazorView> source) 
            => source.SelectMany(template => Observable.FromAsync(async () => {
                template.Preview=await template.GetPreview();
                return template;
            }));

        private static async Task<string> GetPreview(this RazorView.BusinessObjects.RazorView template) 
            => template.ModelType != null && !string.IsNullOrWhiteSpace(template.Template)
                ? await template.Render() : null;

        private static readonly ISubject<GenericEventArgs<(string renderedView, BusinessObjects.RazorView razorView)>> CustomRazorViewRenderingSubject =
                Subject.Synchronize(new Subject<GenericEventArgs<(string renderedView, BusinessObjects.RazorView razorView)>>());

        private static readonly ISubject<GenericEventArgs<(RazorLightEngine engine,BusinessObjects.RazorView razorView, object instance, IObservable<string> renderedView)>> CustomObjectRenderingSubject 
            = Subject.Synchronize(new Subject<GenericEventArgs<(RazorLightEngine engine,BusinessObjects.RazorView razorView, object instance, IObservable<string>renderedView)>>());

        public static IObservable<GenericEventArgs<(string renderedView,BusinessObjects.RazorView razorView)>> WhenRazorViewRendering(this XafApplication application) 
            => CustomRazorViewRenderingSubject.AsObservable();
        
        public static IObservable<GenericEventArgs<(RazorLightEngine engine,BusinessObjects.RazorView razorView, object instance, IObservable<string> renderedView)>> WhenRazorViewDataSourceObjectRendering(this XafApplication application) 
            => CustomObjectRenderingSubject.AsObservable();

        public static async Task<string> Render(this RazorView.BusinessObjects.RazorView razorView) {
            if (!string.IsNullOrWhiteSpace(razorView.Template)) {
                var args = new GenericEventArgs<(string renderedView,BusinessObjects.RazorView razorView)>();
                CustomRazorViewRenderingSubject.OnNext(args);
                if (!args.Handled) {
                    var engine = razorView.NewRazorEngine();
                    razorView.Error = null;
                    var @catch = await razorView.ObjectSpace.GetObjects(razorView.ModelType.Type,
		                    CriteriaOperator.Parse(razorView.ModelCriteria))
	                    .Cast<object>().ToNowObservable()
	                    .SelectMany(o => {
		                    var eventArgs = new GenericEventArgs<(RazorLightEngine engine,BusinessObjects.RazorView razorView, object instance, IObservable<string>
			                    renderedView)>((engine,razorView, o, Observable.Empty<string>()));
		                    CustomObjectRenderingSubject.OnNext(eventArgs);
		                    return eventArgs.Handled ? eventArgs.Instance.renderedView : engine.CompileRenderAsync(razorView.Oid.ToString(), o).ToObservable();
	                    })
	                    .Aggregate((b4, curr) => b4.JoinString("",curr))
	                    .TraceObjectTemplate()
	                    .Catch<string,Exception>(exception => {
		                    razorView.Error = exception.Message;
		                    return Observable.Never<string>();
	                    });
                    return @catch;
                }
                return args.Instance.renderedView;
            }
            return null;
            
        }

        public static RazorLightEngine NewRazorEngine(this BusinessObjects.RazorView template) 
            => new RazorLightEngineBuilder()
                .UseProject(new RazorViewProject(new MemoryStream(template.Template.Bytes())))
                .AddMetadataReferences(MetadataReferences.Select(reference => reference).ToArray())
                .Build();
    }
}