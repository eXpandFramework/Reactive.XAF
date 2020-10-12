using System;
using DevExpress.ExpressApp;
using Xpand.Extensions.XAF.AppDomainExtensions;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.CloneModelView{

	public sealed class CloneModelViewModule : ReactiveModuleBase{
		static CloneModelViewModule(){
			AppDomain.CurrentDomain.AddModelReference("netstandard");
		}

		public CloneModelViewModule(){
			RequiredModuleTypes.Add(typeof(ReactiveModule));
		}

		public override void Setup(ApplicationModulesManager moduleManager){
			base.Setup(moduleManager);
			moduleManager.Connect()
				.Subscribe(this);
		}
	}
}