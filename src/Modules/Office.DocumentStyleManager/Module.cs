using System.Diagnostics;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Office;

using Xpand.XAF.Modules.HideToolBar;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Services;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Services.DocumentStyleManager;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Services.StyleTemplateService;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.SuppressConfirmation;
using Xpand.XAF.Modules.ViewItemValue;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager{
    
    public class DocumentStyleManagerModule:ReactiveModuleBase{
	    static DocumentStyleManagerModule() => TraceSource=new ReactiveTraceSource(nameof(DocumentStyleManagerModule));

        public DocumentStyleManagerModule(){
            RequiredModuleTypes.Add(typeof(ReactiveModule));
            RequiredModuleTypes.Add(typeof(OfficeModule));
            RequiredModuleTypes.Add(typeof(HideToolBarModule));
            RequiredModuleTypes.Add(typeof(ConditionalAppearanceModule));
            RequiredModuleTypes.Add(typeof(SuppressConfirmationModule));
            RequiredModuleTypes.Add(typeof(ViewItemValueModule));
        }

        public override void Setup(ApplicationModulesManager moduleManager){
            base.Setup(moduleManager);
            moduleManager.ShowStyleManager()
                .Merge(moduleManager.DocumentStyleManager())
                .Merge(moduleManager.ApplyTemplateStyle())
                .Merge(moduleManager.DocumentStyleLinkTemplate())
                .Subscribe(this);
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelReactiveModules, IModelReactiveModuleOffice>();
            extenders.Add<IModelOffice, IModelOfficeDocumentStyleManager>();
        }

        public static TraceSource TraceSource{ get; set; }

    }
}