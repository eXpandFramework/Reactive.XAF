using System;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.Reactive.Services.Controllers{
    public static partial class ControllerExtensions{
        public static  IObservable<(NewItemRowListViewController controller, CustomCalculateNewItemRowPositionEventArgs e)>
            WhenCalculateNewItemRowPosition(this NewItemRowListViewController controller) 
            => controller.ProcessEvent<CustomCalculateNewItemRowPositionEventArgs>(nameof(NewItemRowListViewController.CustomCalculateNewItemRowPosition)).InversePair(controller);
    }
}