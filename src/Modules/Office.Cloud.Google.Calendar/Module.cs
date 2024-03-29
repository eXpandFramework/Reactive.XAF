﻿using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base.General;

using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.XAF.ModelExtensions.Shapes;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Office.Cloud.Google.Calendar{
    
    public sealed class GoogleCalendarModule : ReactiveModuleBase{

        static GoogleCalendarModule(){
            TraceSource=new ReactiveTraceSource(nameof(GoogleCalendarModule));
            ModelObjectViewDependencyLogic.AddObjectViewMap(typeof(IModelCalendar),typeof(IEvent));
        }

        public GoogleCalendarModule() => GoogleModule.AddRequirements(this);

        public override void CustomizeLogics(CustomLogics customLogics){
            base.CustomizeLogics(customLogics);
            customLogics.RegisterLogic(typeof(IModelCalendar),typeof(Extensions.Office.Cloud.ModelCalendarLogic));
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelGoogle,IModelGoogleCalendar>();
        }
        
        
        public static ReactiveTraceSource TraceSource{ get; set; }
        public override void Setup(ApplicationModulesManager manager){
            base.Setup(manager);
            manager.Connect()
	            .Subscribe(this);
        }
    }
}
