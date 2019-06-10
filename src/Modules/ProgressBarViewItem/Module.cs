using System;
using System.CodeDom.Compiler;
using System.Linq;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.SystemModule;
using Fasterflect;
using Microsoft.CSharp;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.ProgressBarViewItem{
    public sealed class ProgressBarViewItemModule : ReactiveModuleBase{
        public const string CategoryName = "Xpand.XAF.Modules.ProgressBarViewItem";

        public ProgressBarViewItemModule(){
            RequiredModuleTypes.Add(typeof(SystemModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));   
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
            var codeProvider = new CSharpCodeProvider();
            var compilerParameters = new CompilerParameters{
                CompilerOptions = "/t:library",
                GenerateInMemory = true
            };
            compilerParameters.ReferencedAssemblies.Add("System.dll");
            compilerParameters.ReferencedAssemblies.Add(typeof(ProgressBarViewItemBase).Assembly.Location);
            compilerParameters.ReferencedAssemblies.Add(typeof(ViewItem).Assembly.Location);
            if (callBackHandler!=null){
                var xafWebAssembly = AppDomain.CurrentDomain.GetAssemblies().First(assembly => assembly.FullName.StartsWith("DevExpress.ExpressApp.Web"));
                compilerParameters.ReferencedAssemblies.Add(xafWebAssembly.Location);
            }
            var compilerResults = codeProvider.CompileAssemblyFromSource(compilerParameters,code);
            if (compilerResults.Errors.Count>0){
                var message = string.Join(Environment.NewLine, compilerResults.Errors.Cast<CompilerError>().Select(error => error.ToString()));
                throw new Exception(message);
            }

            var type = compilerResults.CompiledAssembly.Types().First(_ =>!_.IsAbstract&& typeof(ProgressBarViewItemBase).IsAssignableFrom(_));
            editorDescriptorsFactory.List.Add(new ViewItemDescriptor(new ViewItemRegistration(typeof(IModelProgressBarViewItem),type,true)));
        }

    }
}