using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp.SystemModule;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Services.Controllers{
    public static partial class ControllerExtensions{
        public static IObservable<(ListViewProcessCurrentObjectController controller, CustomProcessListViewSelectedItemEventArgs e)> WhenCustomProcessSelectedItem(this ListViewProcessCurrentObjectController controller){
            return Observable.Return(controller).Where(_ => _!=null).CustomProcessSelectedItem();
        }

        public static IObservable<(ListViewProcessCurrentObjectController controller, CustomProcessListViewSelectedItemEventArgs e)> CustomProcessSelectedItem(this IObservable<ListViewProcessCurrentObjectController> controllers){
            return controllers.SelectMany(controller => {
                        return Observable.FromEventPattern<EventHandler<CustomProcessListViewSelectedItemEventArgs>,
                            CustomProcessListViewSelectedItemEventArgs>(h => controller.CustomProcessSelectedItem += h,
                            h => controller.CustomProcessSelectedItem -= h);
//                    .TakeUntil(controller.WhenDeactivated());
                    })
                    .TransformPattern<CustomProcessListViewSelectedItemEventArgs,ListViewProcessCurrentObjectController>()
                ;
        }
    }
}