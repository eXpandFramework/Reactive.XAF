using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.Reactive.Services.Controllers{
    public static partial class ControllerExtensions{
        public static  IObservable<(NewItemRowListViewController controller, CustomCalculateNewItemRowPositionEventArgs e)>
            WhenCalculateNewItemRowPosition(this NewItemRowListViewController controller){
            return Observable.FromEventPattern<EventHandler<CustomCalculateNewItemRowPositionEventArgs>,
                    CustomCalculateNewItemRowPositionEventArgs>(h => controller.CustomCalculateNewItemRowPosition += h,
                    h => controller.CustomCalculateNewItemRowPosition -= h, ImmediateScheduler.Instance)
                .TransformPattern<CustomCalculateNewItemRowPositionEventArgs, NewItemRowListViewController>()
                .TakeUntilDeactivated(controller);
        }
    }
}