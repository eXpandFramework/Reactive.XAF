using System;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.Reactive.Services.Controllers {
    public static class FilterControllerExtensions {
        public static IObservable<(FilterController sender, CreateCustomSearchCriteriaBuilderEventArgs e)> WhenCreateCustomSearchCriteriaBuilder(this FilterController controller) 
            => controller.ProcessEvent<CreateCustomSearchCriteriaBuilderEventArgs>(nameof(FilterController.CreateCustomSearchCriteriaBuilder)).InversePair(controller);
    }
}