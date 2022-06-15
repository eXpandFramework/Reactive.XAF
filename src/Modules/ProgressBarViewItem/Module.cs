using System;
using System.Linq;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.SystemModule;
using Fasterflect;

using Microsoft.CodeAnalysis.CSharp;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Compiler;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.ProgressBarViewItem{
    
    public sealed class ProgressBarViewItemModule : ReactiveModuleBase{
        
        public const string CategoryName = "Xpand.XAF.Modules.ProgressBarViewItem";

        public ProgressBarViewItemModule(){
            RequiredModuleTypes.Add(typeof(SystemModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));   
        }

        public static ReactiveTraceSource TraceSource{ get; set; }
        static ProgressBarViewItemModule(){
            TraceSource=new ReactiveTraceSource(nameof(ProgressBarViewItemModule));
        }
        protected override void RegisterEditorDescriptors(EditorDescriptorsFactory editorDescriptorsFactory){
            base.RegisterEditorDescriptors(editorDescriptorsFactory);
            string callBackHandler = null;
            
            var references = new []{typeof(ProgressBarViewItemBase).Assembly.Location,typeof(ViewItem).Assembly.Location};
            if (Application != null && Application.GetPlatform() == Platform.Web){
                callBackHandler = ",DevExpress.ExpressApp.Web.Templates.IXafCallbackHandler";
                var xafWebAssembly = AppDomain.CurrentDomain.GetAssemblies().First(assembly => assembly.FullName.StartsWith("DevExpress.ExpressApp.Web"));
                references = references.Concat(xafWebAssembly.Location.YieldItem()).ToArray();
            }
            else {
                var location = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.FullName.StartsWith("System.Private.CoreLib"))?.Location;
                if (location != null) {
                    references = references.Concat(location.YieldItem()).ToArray();
                }
            }
            string code = $@"
namespace {GetType().Namespace}{{
    public class ProgressBarViewItem:{typeof(ProgressBarViewItemBase).FullName}{callBackHandler}{{
        public ProgressBarViewItem({typeof(IModelProgressBarViewItem).FullName} info, System.Type classType) : base(info, classType){{
        }}
    }}
}}
";
            
            using var memoryStream = CSharpSyntaxTree.ParseText(code).Compile(references);
            var type = AppDomain.CurrentDomain.LoadAssembly(memoryStream).Types().First(_ => !_.IsAbstract && typeof(ProgressBarViewItemBase).IsAssignableFrom(_));
            editorDescriptorsFactory.List.Add(new ViewItemDescriptor(new ViewItemRegistration(typeof(IModelProgressBarViewItem),type,true)));
        }

    }
}