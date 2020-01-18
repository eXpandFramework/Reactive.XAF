using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp.SystemModule;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Services.Controllers{
    public static partial class ControllerExtensions{
        public static IObservable<EventPattern<CustomizeWindowStatusMessagesEventArgs>> WhenCustomizeWindowStatusMessages(this WindowTemplateController controller){
            return Observable.Return(controller).Where(_ => _!=null).CustomizeWindowStatusMessages();
        }

        public static IObservable<EventPattern<CustomizeWindowStatusMessagesEventArgs>>  CustomizeWindowStatusMessages(this IObservable<WindowTemplateController> controllers){
            return controllers.SelectMany(controller => {
                return Observable.FromEventPattern<EventHandler<CustomizeWindowStatusMessagesEventArgs>,
                        CustomizeWindowStatusMessagesEventArgs>(h => controller.CustomizeWindowStatusMessages += h,
                        h => controller.CustomizeWindowStatusMessages -= h,ImmediateScheduler.Instance).TraceRX();
            });
        }
    }
}