using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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
            => controller.ProcessEvent<CustomProcessListViewSelectedItemEventArgs>(nameof(ListViewProcessCurrentObjectController.CustomProcessSelectedItem))
                .DoWhen(_ => handled.HasValue,eventArgs => eventArgs.Handled=handled.Value)
                .Select(eventArgs => eventArgs.InnerArgs);
        public static IObservable<SimpleActionExecuteEventArgs> WhenCustomProcessSelectedItem(
            this ListViewProcessCurrentObjectController controller,Func<SimpleActionExecuteEventArgs,bool> overwrite) 
            => controller.ProcessEvent<CustomProcessListViewSelectedItemEventArgs>(nameof(ListViewProcessCurrentObjectController.CustomProcessSelectedItem))
                .Do(e => e.Handled=overwrite(e.InnerArgs)).Where(e => e.Handled)
                .Select(eventArgs => eventArgs.InnerArgs);
        
        [SuppressMessage("ReSharper", "PossibleInvalidOperationException")]
        public static IObservable<HandledEventArgs> WhenCustomHandleProcessSelectedItem(
            this ListViewProcessCurrentObjectController controller,bool? handled=null) 
            => controller.ProcessEvent<HandledEventArgs>(nameof(ListViewProcessCurrentObjectController.CustomHandleProcessSelectedItem))
                .DoWhen(_ => handled.HasValue,eventArgs => eventArgs.Handled=handled.Value)
                .Select(eventArgs => eventArgs);

        public static IObservable<SimpleActionExecuteEventArgs> CustomProcessSelectedItem(this IObservable<ListViewProcessCurrentObjectController> source,bool? handled=null) 
            => source.SelectMany(controller => controller.WhenCustomProcessSelectedItem(handled));
        public static IObservable<SimpleActionExecuteEventArgs> CustomProcessSelectedItem(this IObservable<ListViewProcessCurrentObjectController> source,Func<SimpleActionExecuteEventArgs,bool> overwrite) 
            => source.SelectMany(controller => controller.WhenCustomProcessSelectedItem(overwrite));
    }
}