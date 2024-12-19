using System.ComponentModel;
using DevExpress.ExpressApp;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.ModelEditor.Module.Win {
    [ToolboxItemFilter("Xaf.Platform.Win")]
    public sealed class ModelEditorWindowsFormsModule : ReactiveModuleBase {
	    
        public ModelEditorWindowsFormsModule() {
	        RequiredModuleTypes.Add(typeof(Modules.Reactive.ReactiveModule));
            RequiredModuleTypes.Add(typeof(Modules.OneView.OneViewModule));
            RequiredModuleTypes.Add(typeof(Modules.Windows.WindowsModule));
            RequiredModuleTypes.Add(typeof(Modules.GridListEditor.GridListEditorModule));
            RequiredModuleTypes.Add(typeof(Modules.ModelMapper.ModelMapperModule));
            
            // RequiredModuleTypes.Add(typeof(Modules.Reactive.Logger.Hub.ReactiveLoggerHubModule));
        }
        static ModelEditorWindowsFormsModule(){
	        TraceSource=new ReactiveTraceSource(nameof(ModelEditorWindowsFormsModule));
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public static ReactiveTraceSource TraceSource{ get; set; }

        public override void Setup(ApplicationModulesManager moduleManager) {
            base.Setup(moduleManager);
            moduleManager.Connect()
                .Subscribe(this);
            moduleManager.Extend(PredefinedMap.GridView);
            
        }
    }
}
