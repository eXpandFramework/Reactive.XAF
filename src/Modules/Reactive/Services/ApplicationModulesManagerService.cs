using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ApplicationModulesManagerExtensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class ApplicationModulesManagerService{
        public static IObservable<XafApplication> WhenApplication(this ApplicationModulesManager manager){
            return manager.WhereApplication().ToObservable();
        }

        public static IObservable<(ApplicationModulesManager manager, CustomizeTypesInfoEventArgs e)> WhenCustomizeTypesInfo(this ApplicationModulesManager manager){
            return Observable.FromEventPattern<EventHandler<CustomizeTypesInfoEventArgs>, CustomizeTypesInfoEventArgs>(
                h => manager.CustomizeTypesInfo += h, h => manager.CustomizeTypesInfo += h, ImmediateScheduler.Instance)
                .TransformPattern<CustomizeTypesInfoEventArgs,ApplicationModulesManager>();
        }
    }
}