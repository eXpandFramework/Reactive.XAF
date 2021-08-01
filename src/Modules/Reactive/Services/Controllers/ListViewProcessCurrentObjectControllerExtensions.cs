using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.Reactive.Services.Controllers{
    public static partial class ControllerExtensions{

        public static IObservable<(ListViewProcessCurrentObjectController controller, CustomProcessListViewSelectedItemEventArgs e)> WhenCustomProcessSelectedItem(this ListViewProcessCurrentObjectController controller,bool? handled=null) 
            => controller.ReturnObservable().Where(_ => _!=null).CustomProcessSelectedItem()
	            .Do(t => {
		            if (handled.HasValue) {
			            t.e.Handled = handled.Value;
		            }
	            })
            ;

        public static IObservable<(ListViewProcessCurrentObjectController controller, CustomProcessListViewSelectedItemEventArgs e
                )> CustomProcessSelectedItem(this IObservable<ListViewProcessCurrentObjectController> source) 
            => source.SelectMany(controller => Observable.FromEventPattern<EventHandler<CustomProcessListViewSelectedItemEventArgs>,
                    CustomProcessListViewSelectedItemEventArgs>(h => controller.CustomProcessSelectedItem += h,
                    h => controller.CustomProcessSelectedItem -= h,ImmediateScheduler.Instance))
                .TransformPattern<CustomProcessListViewSelectedItemEventArgs,ListViewProcessCurrentObjectController>();
    }
}