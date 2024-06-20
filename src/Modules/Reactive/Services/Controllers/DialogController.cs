using System;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.Reactive.Utility;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Reactive.Services.Controllers {
    public static partial class ControllerExtensions {
        public static IObservable<T> WhenAcceptTriggered<T>(this IObservable<DialogController> source,IObservable<T> afterExecuted,params object[] selection) 
            => source.SelectMany(controller => controller.AcceptAction.Trigger(afterExecuted,selection).Take(1));
        public static IObservable<Unit> WhenAcceptTriggered(this IObservable<DialogController> source) 
            => source.SelectMany(controller => controller.WhenAcceptTriggered().Take(1));

        public static IObservable<Unit> WhenAcceptTriggered(this DialogController controller) 
            => controller.AcceptAction.Trigger();

        public static IObservable<Unit> WhenCancelTriggered(this IObservable<DialogController> source) 
            => source.SelectMany(controller => controller.CancelAction.Trigger().Take(1));
    }
}