using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.Reactive.Services.Controllers{
    public static partial class ControllerExtensions{
        public static IObservable<CustomizeWindowStatusMessagesEventArgs> WhenCustomizeWindowStatusMessages(this WindowTemplateController controller) 
            => controller.ProcessEvent<CustomizeWindowStatusMessagesEventArgs>(nameof(WindowTemplateController.CustomizeWindowStatusMessages));
        public static IObservable<CustomizeWindowCaptionEventArgs> WhenCustomizeWindowCaption(this WindowTemplateController controller) 
            => controller.ProcessEvent<CustomizeWindowCaptionEventArgs>(nameof(WindowTemplateController.CustomizeWindowCaption));

        public static IObservable<CustomizeWindowStatusMessagesEventArgs> CustomizeWindowStatusMessages(this IObservable<WindowTemplateController> controllers) 
            => controllers.SelectMany(controller => controller.WhenCustomizeWindowStatusMessages());
    }
}