using System;
using System.Reactive;
using DevExpress.ExpressApp;

namespace Xpand.XAF.Modules.Reactive.Services {
    public static partial class AttributesExtensions {
        private static IObservable<Unit> PreventAggregatedObjectsValidationAttribute(this ApplicationModulesManager manager) 
            => manager.WhenApplication(application => application.WhenSetupComplete(xafApplication => xafApplication.PreventAggregatedObjectsValidationAttribute()));
    }}