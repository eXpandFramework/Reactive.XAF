using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
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
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.ObjectTemplate.Template;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.ObjectTemplate{
    public static class ObjectTemplateService {
        public static readonly ConcurrentHashSet<MetadataReference> MetadataReferences;

        static ObjectTemplateService() 
            => MetadataReferences = new ConcurrentHashSet<MetadataReference>(Directory.GetFiles(AppDomain.CurrentDomain.ApplicationPath(), "*.dll").ToNowObservable()
                .SelectMany(path => Observable.Start(() => MetadataReference.CreateFromFile(path))
                    .OnErrorResumeNext(Observable.Empty<MetadataReference>())).ToEnumerable());

        internal static IObservable<TSource> TraceObjectTemplate<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, ObjectTemplateModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);
        
        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager)
            => manager.WhenApplication(application => application.WhenSetupComplete()
                    .SelectMany(_ => application.RenderPreview())).ToUnit()
                .Merge(ConfigureRichEditFormat(manager));

        private static IObservable<Unit> ConfigureRichEditFormat(this ApplicationModulesManager manager) 
            => manager.WhenGeneratingModelNodes<IModelBOModel>()
                .SelectMany(model => {
                    var modelClass = model.GetClass(typeof(BusinessObjects.ObjectTemplate));
                    return new[] { nameof(BusinessObjects.ObjectTemplate.Preview), nameof(BusinessObjects.ObjectTemplate.Template) }.ToNowObservable()
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
            => application.WhenFrameViewChanged()
                .WhenFrame(typeof(BusinessObjects.ObjectTemplate)).WhenFrame(ViewType.DetailView)
                .SelectMany(frame => {
                    var objectTemplate = ((BusinessObjects.ObjectTemplate)frame.View.CurrentObject);
                    return objectTemplate.Render().ToObservable().Do(s => objectTemplate.Preview=s).ToUnit()
                        .Concat(frame.View.ObjectSpace.MonitorObjectTemplateChange().RenderPreview().ToUnit());
                });

        private static IObservable<BusinessObjects.ObjectTemplate> MonitorObjectTemplateChange(this IObjectSpace objectSpace)
            => objectSpace.WhenObjectChanged().Where(t 
                    => new[] {
                            nameof(BusinessObjects.ObjectTemplate.Template), nameof(BusinessObjects.ObjectTemplate.ModelType),
                            nameof(BusinessObjects.ObjectTemplate.ModelCriteria)
                        }
                        .Contains(t.e.PropertyName)&&t.e.Object is BusinessObjects.ObjectTemplate)
                .Select(t => t.e.Object).Cast<BusinessObjects.ObjectTemplate>();
        
        static IObservable<BusinessObjects.ObjectTemplate> RenderPreview(this IObservable<BusinessObjects.ObjectTemplate> source) 
            => source.SelectMany(template => Observable.FromAsync(async () => {
                template.Preview=await template.GetPreview();
                return template;
            }));

        private static async Task<string> GetPreview(this BusinessObjects.ObjectTemplate template) 
            => template.ModelType != null && !string.IsNullOrWhiteSpace(template.Template)
                ? await template.Render() : null;

        private static readonly ISubject<GenericEventArgs<string>> CustomTemplateRenderSubject = Subject.Synchronize(new Subject<GenericEventArgs<string>>());

        public static IObservable<GenericEventArgs<string>> CustomTemplateRender => CustomTemplateRenderSubject.AsObservable();

        public static async Task<string> Render(this BusinessObjects.ObjectTemplate template) {
            if (!string.IsNullOrWhiteSpace(template.Template)) {
                var args = new GenericEventArgs<string>();
                CustomTemplateRenderSubject.OnNext(args);
                if (!args.Handled) {
                    var engine = new RazorLightEngineBuilder()
                        .UseProject(new ObjectTemplateRazorProject(new MemoryStream(template.Template.Bytes())))
                        .AddMetadataReferences(MetadataReferences.Select(reference => reference).ToArray())
                        .Build();
                    template.Error = null;
                    return await template.ObjectSpace.GetObjects(template.ModelType.Type,
                            CriteriaOperator.Parse(template.ModelCriteria))
                        .Cast<object>().ToNowObservable()
                        .SelectMany(o => engine.CompileRenderAsync(template.Oid.ToString(), o))
                        .Aggregate((b4, curr) => b4.JoinString("",curr))
                        .TraceObjectTemplate()
                        .Catch<string,Exception>(exception => {
                            template.Error = exception.Message;
                            return Observable.Never<string>();
                        });
    
                }
                return args.Instance;
            }
            return null;
            
        }
    }
}