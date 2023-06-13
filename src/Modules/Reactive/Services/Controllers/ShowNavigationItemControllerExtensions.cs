using System;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.Reactive.Services.Controllers{
    public static class ShowNavigationItemControllerExtensions{
        public static IObservable<ShowNavigationItemController> WhenCustomShowNavigationItem(this ShowNavigationItemController controller) 
            => controller.WhenEvent<ShowNavigationItemController>(nameof(ShowNavigationItemController.CustomShowNavigationItem));

        public static IObservable<CustomGetStartupNavigationItemEventArgs> WhenCustomGetStartupNavigationItem(this ShowNavigationItemController controller) 
            => controller.WhenEvent<CustomGetStartupNavigationItemEventArgs>(nameof(ShowNavigationItemController.CustomGetStartupNavigationItem));
    }
}