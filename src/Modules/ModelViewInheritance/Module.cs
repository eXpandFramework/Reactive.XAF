using DevExpress.ExpressApp;

using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.ModelViewInheritance {
    
    public sealed class ModelViewInheritanceModule : ReactiveModuleBase {
        public ModelViewInheritanceModule() {
            RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.SystemModule.SystemModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));
        }


        public override void Setup(ApplicationModulesManager moduleManager) {
            base.Setup(moduleManager);
            moduleManager.Connect().Subscribe(this);
        }
    }
}
