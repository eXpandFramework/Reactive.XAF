using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Services.Controllers{
    public static partial class ControllerExtensions{
        public static IObservable<EventPattern<CustomizeWindowStatusMessagesEventArgs>> WhenCustomizeWindowStatusMessages(this WindowTemplateController controller) 
            => controller.ReturnObservable().Where(_ => _!=null).CustomizeWindowStatusMessages();

        public static IObservable<EventPattern<CustomizeWindowStatusMessagesEventArgs>>  CustomizeWindowStatusMessages(this IObservable<WindowTemplateController> controllers) 
            => controllers.SelectMany(controller => Observable.FromEventPattern<EventHandler<CustomizeWindowStatusMessagesEventArgs>,
			        CustomizeWindowStatusMessagesEventArgs>(h => controller.CustomizeWindowStatusMessages += h,
			        h => controller.CustomizeWindowStatusMessages -= h,ImmediateScheduler.Instance))
		        .TraceRX(_ => string.Join(", ",_.EventArgs.StatusMessages));
    }
}