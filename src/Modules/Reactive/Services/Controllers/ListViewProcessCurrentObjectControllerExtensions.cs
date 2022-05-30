using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.XAF.Modules.Reactive.Services.Controllers{
    public static partial class ControllerExtensions{

        [SuppressMessage("ReSharper", "PossibleInvalidOperationException")]
        public static IObservable<SimpleActionExecuteEventArgs> WhenCustomProcessSelectedItem(
            this ListViewProcessCurrentObjectController controller,bool? handled=null) 
            => Observable.FromEventPattern<EventHandler<CustomProcessListViewSelectedItemEventArgs>,
                CustomProcessListViewSelectedItemEventArgs>(h => controller.CustomProcessSelectedItem += h,
                h => controller.CustomProcessSelectedItem -= h,ImmediateScheduler.Instance)
                .TransformPattern<CustomProcessListViewSelectedItemEventArgs,ListViewProcessCurrentObjectController>()
                .DoWhen(_ => handled.HasValue,e => e.e.Handled=handled.Value)
                .Select(t => t.e.InnerArgs);

        public static IObservable<SimpleActionExecuteEventArgs> CustomProcessSelectedItem(this IObservable<ListViewProcessCurrentObjectController> source,bool? handled=null) 
            => source.SelectMany(controller => controller.WhenCustomProcessSelectedItem(handled));
    }
}