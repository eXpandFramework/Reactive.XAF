using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.StoreToDisk {
    public sealed class StoreToDiskModule : ReactiveModuleBase {
        public static ReactiveTraceSource TraceSource { get; set; }

        static StoreToDiskModule() => TraceSource = new ReactiveTraceSource(nameof(StoreToDiskModule));

        public StoreToDiskModule() {
            RequiredModuleTypes.Add(typeof(SystemModule));
            RequiredModuleTypes.Add(typeof(ReactiveModule));
        }
        
        public override void Setup(XafApplication application) {
            base.Setup(application);
            application.Connect().Finally(() => {}).Subscribe(this);
        }

    }
}