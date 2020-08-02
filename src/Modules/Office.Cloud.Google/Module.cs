using System.Diagnostics;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Office.Cloud.BusinessObjects;
using Xpand.XAF.Modules.Office.Cloud.Google.BusinessObjects;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Office.Cloud.Google{
    public class GoogleModule:ReactiveModuleBase{
	    static GoogleModule() => TraceSource=new ReactiveTraceSource(nameof(GoogleModule));

        public GoogleModule() => RequiredModuleTypes.Add(typeof(ReactiveModule));

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            moduleManager.Connect()
                .Subscribe(this);
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelReactiveModules, IModelReactiveModuleOffice>();
            extenders.Add<IModelOffice, IModelOfficeGoogle>();
        }

        public static TraceSource TraceSource{ get; set; }

        public static void AddRequirements(ModuleBase module){
            module.RequiredModuleTypes.Add(typeof(GoogleModule));
            module.AdditionalExportedTypes.Add(typeof(GoogleAuthentication));
            module.AdditionalExportedTypes.Add(typeof(CloudOfficeObject));
            module.AdditionalExportedTypes.Add(typeof(CloudOfficeTokenStorage));
        }
    }
}