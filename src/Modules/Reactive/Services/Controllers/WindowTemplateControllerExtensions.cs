using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp.SystemModule;

namespace Xpand.XAF.Modules.Reactive.Services.Controllers{
    public static partial class ControllerExtensions{
        public static IObservable<EventPattern<CustomizeWindowStatusMessagesEventArgs>> WhenCustomizeWindowStatusMessages(this WindowTemplateController controller) 
            => Observable.FromEventPattern<EventHandler<CustomizeWindowStatusMessagesEventArgs>,
                CustomizeWindowStatusMessagesEventArgs>(h => controller.CustomizeWindowStatusMessages += h,
                h => controller.CustomizeWindowStatusMessages -= h,ImmediateScheduler.Instance);
        public static IObservable<CustomizeWindowCaptionEventArgs> WhenCustomizeWindowCaption(this WindowTemplateController controller) 
            => Observable.FromEventPattern<EventHandler<CustomizeWindowCaptionEventArgs>, CustomizeWindowCaptionEventArgs>(h => controller.CustomizeWindowCaption += h,
                h => controller.CustomizeWindowCaption -= h,ImmediateScheduler.Instance).Select(pattern => pattern.EventArgs);

        public static IObservable<EventPattern<CustomizeWindowStatusMessagesEventArgs>>  CustomizeWindowStatusMessages(this IObservable<WindowTemplateController> controllers) 
            => controllers.SelectMany(controller => controller.WhenCustomizeWindowStatusMessages());
    }
}