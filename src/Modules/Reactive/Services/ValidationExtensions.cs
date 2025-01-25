using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Validation;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.Attributes.Validation;
using Xpand.Extensions.XAF.TypesInfoExtensions;

namespace Xpand.XAF.Modules.Reactive.Services {
    public static class ValidationExtensions {
        internal static IObservable<Unit> PreventAggregatedObjectsValidationAttribute(this XafApplication application) {
            return application.WhenFrame(application.TypesInfo.PersistentTypes
                    .Attributed<PreventAggregatedObjectsValidationAttribute>().Select(t => t.typeInfo.Type).ToArray())
                .SelectMany(frame => frame.WhenController<PersistenceValidationController>()
                    .SelectMany(controller => controller.WhenEvent<CustomGetAggregatedObjectsToValidateEventArgs>(nameof(PersistenceValidationController.CustomGetAggregatedObjectsToValidate)))
                    .Do(e => {
                        e.AggregatedObjects.Clear();
                        e.Handled = true;
                    }))
                .ToUnit();
                
                
        }

    }
}