using DevExpress.ExpressApp;
using JetBrains.Annotations;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Rest{

	public sealed class RestModule : ReactiveModuleBase{
        static RestModule(){
            TraceSource=new ReactiveTraceSource(nameof(RestModule));
        }
        [PublicAPI]
        public static ReactiveTraceSource TraceSource{ get; set; }
		public RestModule(){
			RequiredModuleTypes.Add(typeof(ReactiveModule));
		}

		public override void Setup(ApplicationModulesManager moduleManager){
			base.Setup(moduleManager);
			moduleManager.Connect()
				.Subscribe(this);
		}
	}
}