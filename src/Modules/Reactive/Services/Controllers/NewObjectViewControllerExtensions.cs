using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.Reactive.Services.Controllers{
    public static partial class ControllerExtensions{
        public static IObservable<(NewObjectViewController sender, CollectTypesEventArgs e)> WhenCollectCreatableItemTypes(this NewObjectViewController controller) 
            => controller.ReturnObservable().Where(_ => _!=null).CollectCreatableItemTypes();

        public static IObservable<(NewObjectViewController sender, CollectTypesEventArgs e)> CollectCreatableItemTypes(
            this IObservable<NewObjectViewController> source)
            => source.SelectMany(controller =>
                    Observable.FromEventPattern<EventHandler<CollectTypesEventArgs>, CollectTypesEventArgs>(
                        h => controller.CollectCreatableItemTypes += h,
                        h => controller.CollectCreatableItemTypes -= h, ImmediateScheduler.Instance))
                .TransformPattern<CollectTypesEventArgs, NewObjectViewController>();
    }
}