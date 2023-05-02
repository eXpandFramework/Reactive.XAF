using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.StoreToDisk {
    public sealed class StoreToDiskModule : ReactiveModuleBase {
        public static ReactiveTraceSource TraceSource { get; set; }

        static StoreToDiskModule() {
            TraceSource = new ReactiveTraceSource(nameof(StoreToDiskModule));
        }

        public StoreToDiskModule() {
            RequiredModuleTypes.Add(typeof(SystemModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));
        }

        public override void Setup(ApplicationModulesManager moduleManager) {
            base.Setup(moduleManager);
            moduleManager.Connect().Finally(() => {}).Subscribe(this);
        }

        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders) {
            base.ExtendModelInterfaces(extenders);
            extenders.Add<IModelReactiveModules, IModelReactiveModulesStoreToDisk>();
        }
    }
}