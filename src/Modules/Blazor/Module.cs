using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.SystemModule;
using JetBrains.Annotations;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Blazor {
    [UsedImplicitly]
    public sealed class BlazorModule : ReactiveModuleBase {

        static BlazorModule() => TraceSource=new ReactiveTraceSource(nameof(BlazorModule));

        [PublicAPI]
        public static ReactiveTraceSource TraceSource{ get; set; }
        public BlazorModule() {
            RequiredModuleTypes.Add(typeof(ReactiveModule));
            RequiredModuleTypes.Add(typeof(SystemBlazorModule));
        }

        public override void Setup(ApplicationModulesManager moduleManager) {
            base.Setup(moduleManager);
            moduleManager.Connect()
                .Subscribe(this);
        }
    }
}
