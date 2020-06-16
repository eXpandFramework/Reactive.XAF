using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ApplicationModulesManagerExtensions;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class ApplicationModulesManagerService{
	    public static IObservable<Unit> WhenApplication(this ApplicationModulesManager manager,Func<XafApplication,IObservable<Unit>> selector) => manager
		    .WhereApplication().ToObservable(ImmediateScheduler.Instance)
		    .SelectMany(application => selector(application).Retry(application));

	    public static IObservable<(ApplicationModulesManager manager, CustomizeTypesInfoEventArgs e)> WhenCustomizeTypesInfo(this ApplicationModulesManager manager) =>
	        Observable.FromEventPattern<EventHandler<CustomizeTypesInfoEventArgs>, CustomizeTypesInfoEventArgs>(
			        h => manager.CustomizeTypesInfo += h, h => manager.CustomizeTypesInfo += h, ImmediateScheduler.Instance)
		        .TransformPattern<CustomizeTypesInfoEventArgs,ApplicationModulesManager>();
    }
}