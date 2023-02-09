using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Xpand.Extensions.AppDomainExtensions;
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
	        // AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
            TraceSource=new ReactiveTraceSource(nameof(ModelEditorWindowsFormsModule));
        }

        private static Assembly CurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args){
	        var name = args.Name;
	        if (name.Contains("Host")) {

	        }
	        var comma = name.IndexOf(",", StringComparison.Ordinal);
	        if (comma > -1){
		        name = args.Name.Substring(0, comma);
	        }
        
	        try {
		        var path = $@"{AppDomain.CurrentDomain.ApplicationPath()}\{name}.dll";
		        return File.Exists(path) ? Assembly.LoadFile(path) : null;
	        }
	        catch (Exception e){
		        Tracing.Tracer.LogError(e);
		        return null;
	        }
        }

        
        public static ReactiveTraceSource TraceSource{ get; set; }

        public override void Setup(ApplicationModulesManager moduleManager) {
            base.Setup(moduleManager);
            moduleManager.Connect()
                .Subscribe(this);
            moduleManager.Extend(PredefinedMap.GridView);
            
        }
    }
}
