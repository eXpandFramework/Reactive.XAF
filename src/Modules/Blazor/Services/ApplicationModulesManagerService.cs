using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using Xpand.Extensions.Blazor;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Blazor.Services {
    public static class ApplicationModulesManagerService {
        public static IObservable<BlazorApplication> WhenNotSharedApplication<T>(this ApplicationModulesManager manager,Func<XafApplication,IObservable<T>> retriedExecution) 
            => manager.WhenApplication(application => {
                if (application is ISharedBlazorApplication blazorApplication &&
                    !blazorApplication.UseNonSecuredObjectSpaceProvider) {
                    return application.ReturnObservable();
                }
                return Observable.Empty<XafApplication>();
            }).Cast<BlazorApplication>();

    }
}