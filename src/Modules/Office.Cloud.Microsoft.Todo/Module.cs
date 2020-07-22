using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base.General;
using JetBrains.Annotations;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft.Todo{
    [UsedImplicitly]
    public sealed class MicrosoftTodoModule : ReactiveModuleBase{
        [PublicAPI]
        public const string ModelCategory = "Xpand.MicrosoftTodo";
        
        static MicrosoftTodoModule(){
            TraceSource=new ReactiveTraceSource(nameof(MicrosoftTodoModule));
            ModelObjectViewDependencyLogic.ObjectViewsMap.Add(typeof(IModelTodo),typeof(ITask));
        }

        public MicrosoftTodoModule() => MicrosoftModule.AddRequirements(this);

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelMicrosoft,IModelMicrosoftTodo>();
        }
        
        [PublicAPI]
        public static ReactiveTraceSource TraceSource{ get; set; }
        public override void Setup(ApplicationModulesManager manager){
            base.Setup(manager);
            manager.Connect()
	            .Subscribe(this);
        }
    }
}
