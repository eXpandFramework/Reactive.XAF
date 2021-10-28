using System;
using System.ComponentModel;
using System.Reactive.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.RazorView{
	public interface IModelReactiveModulesRazorView : IModelReactiveModule{
		[Browsable(false)]
		IModelRazorView RazorView{ get; }
	}

	[DomainLogic(typeof(IModelRazorView))]
	public static class ModelRazorViewLogic{
		
		public static IObservable<IModelRazorView> RazorView(this IObservable<IModelReactiveModules> source) 
			=> source.Select(modules => modules.RazorView());

		public static IModelRazorView RazorView(this IModelReactiveModules reactiveModules) 
			=> ((IModelReactiveModulesRazorView) reactiveModules).RazorView;
		internal static IModelRazorView ModelRazorView(this IModelApplication modelApplication) 
			=> modelApplication.ToReactiveModule<IModelReactiveModulesRazorView>().RazorView;

	}

	public interface IModelRazorView : IModelNode{


	}
	
}