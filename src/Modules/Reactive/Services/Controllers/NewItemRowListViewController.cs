using System;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Reactive.Services.Controllers{
    public static partial class ControllerExtensions{
        public static  IObservable<(NewItemRowListViewController controller, CustomCalculateNewItemRowPositionEventArgs e)>
            WhenCalculateNewItemRowPosition(this NewItemRowListViewController controller) 
            => controller.WhenEvent<CustomCalculateNewItemRowPositionEventArgs>(nameof(NewItemRowListViewController.CustomCalculateNewItemRowPosition)).InversePair(controller);
    }
}