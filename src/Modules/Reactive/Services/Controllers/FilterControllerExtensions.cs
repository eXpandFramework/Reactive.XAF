using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.Reactive.Services.Controllers {
    public static class FilterControllerExtensions {
        public static IObservable<(FilterController sender, CreateCustomSearchCriteriaBuilderEventArgs e)> WhenCreateCustomSearchCriteriaBuilder(this FilterController controller) 
            => Observable
                .FromEventPattern<EventHandler<CreateCustomSearchCriteriaBuilderEventArgs>,
                    CreateCustomSearchCriteriaBuilderEventArgs>(h => controller.CreateCustomSearchCriteriaBuilder += h,
                    h => controller.CreateCustomSearchCriteriaBuilder -= h)
                .TransformPattern<CreateCustomSearchCriteriaBuilderEventArgs, FilterController>();
    }
}