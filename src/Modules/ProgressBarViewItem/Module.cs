using System;
using System.Linq;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.SystemModule;
using Fasterflect;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CSharp;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Compiler;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.ProgressBarViewItem{
    [UsedImplicitly]
    public sealed class ProgressBarViewItemModule : ReactiveModuleBase{
        [PublicAPI]
        public const string CategoryName = "Xpand.XAF.Modules.ProgressBarViewItem";

        public ProgressBarViewItemModule(){
            RequiredModuleTypes.Add(typeof(SystemModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));   
        }

        public static ReactiveTraceSource TraceSource{ get; [PublicAPI]set; }
        static ProgressBarViewItemModule(){
            TraceSource=new ReactiveTraceSource(nameof(ProgressBarViewItemModule));
        }
        protected override void RegisterEditorDescriptors(EditorDescriptorsFactory editorDescriptorsFactory){
            base.RegisterEditorDescriptors(editorDescriptorsFactory);
            string callBackHandler = null;
            if (Application != null && Application.GetPlatform() == Platform.Web){
                callBackHandler = ",DevExpress.ExpressApp.Web.Templates.IXafCallbackHandler";
            }
            string code = $@"
namespace {GetType().Namespace}{{
    public class ProgressBarViewItem:{typeof(ProgressBarViewItemBase).FullName}{callBackHandler}{{
        public ProgressBarViewItem({typeof(IModelProgressBarViewItem).FullName} info, System.Type classType) : base(info, classType){{
        }}
    }}
}}
";
            var references = new []{typeof(ProgressBarViewItemBase).Assembly.Location,typeof(ViewItem).Assembly.Location};
            if (callBackHandler!=null){
                var xafWebAssembly = AppDomain.CurrentDomain.GetAssemblies().First(assembly => assembly.FullName.StartsWith("DevExpress.ExpressApp.Web"));
                references = references.Add(xafWebAssembly.Location);
            }
            else {
                var location = AppDomain.CurrentDomain.GetAssemblies().First(assembly => assembly.FullName.StartsWith("System.Private.CoreLib")).Location;
                references = references.Add(location);
            }

            using var memoryStream = CSharpSyntaxTree.ParseText(code).Compile(references);
            var type = AppDomain.CurrentDomain.LoadAssembly(memoryStream).Types().First(_ => !_.IsAbstract && typeof(ProgressBarViewItemBase).IsAssignableFrom(_));
            editorDescriptorsFactory.List.Add(new ViewItemDescriptor(new ViewItemRegistration(typeof(IModelProgressBarViewItem),type,true)));
        }

    }
}