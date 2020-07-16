using System.Diagnostics;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Office.Cloud.BusinessObjects;
using Xpand.XAF.Modules.Office.Cloud.Microsoft.BusinessObjects;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft{
    public class MicrosoftModule:ReactiveModuleBase{
	    static MicrosoftModule() => TraceSource=new ReactiveTraceSource(nameof(MicrosoftModule));

        public MicrosoftModule() => RequiredModuleTypes.Add(typeof(ReactiveModule));

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            moduleManager.Connect()
                .Subscribe(this);
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelReactiveModules, IModelReactiveModuleOffice>();
            extenders.Add<IModelOffice, IModelOfficeMicrosoft>();
        }

        public static TraceSource TraceSource{ get; set; }

        public static void AddRequirements(ModuleBase module){
            module.RequiredModuleTypes.Add(typeof(MicrosoftModule));
            module.AdditionalExportedTypes.Add(typeof(MSAuthentication));
            module.AdditionalExportedTypes.Add(typeof(CloudOfficeObject));
            module.AdditionalExportedTypes.Add(typeof(CloudOfficeTokenStorage));
        }
    }
}