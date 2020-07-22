using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base.General;
using JetBrains.Annotations;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft.Calendar{
    [UsedImplicitly]
    public sealed class MicrosoftCalendarModule : ReactiveModuleBase{
        [PublicAPI]
        public const string ModelCategory = "Xpand.MicrosoftCalendar";
        
        static MicrosoftCalendarModule(){
            TraceSource=new ReactiveTraceSource(nameof(MicrosoftCalendarModule));
            ModelObjectViewDependencyLogic.ObjectViewsMap.Add(typeof(IModelCalendar),typeof(IEvent));
        }

        public MicrosoftCalendarModule() => MicrosoftModule.AddRequirements(this);
        
        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelMicrosoft,IModelMicrosoftCalendar>();
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
