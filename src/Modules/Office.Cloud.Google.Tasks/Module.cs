using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base.General;

using Xpand.Extensions.XAF.ModelExtensions.Shapes;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Office.Cloud.Google.Tasks{
    
    public sealed class GoogleTasksModule : ReactiveModuleBase{

        static GoogleTasksModule(){
            TraceSource=new ReactiveTraceSource(nameof(GoogleTasksModule));
            // ModelObjectViewDependencyLogic.AddObjectViewMap(typeof(IModelTasks),typeof(ITask));
        }

        public GoogleTasksModule() => GoogleModule.AddRequirements(this);

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelGoogle,IModelGoogleTasks>();
        }
        
        
        public static ReactiveTraceSource TraceSource{ get; set; }
        public override void Setup(ApplicationModulesManager manager){
            base.Setup(manager);
            manager.Connect()
	            .Subscribe(this);
        }
    }
}
