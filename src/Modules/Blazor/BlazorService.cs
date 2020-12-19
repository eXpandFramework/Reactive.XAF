using System;
using System.Reactive;
using DevExpress.ExpressApp;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Blazor {
    public static class BlazorService {
        internal static  IObservable<Unit> Connect(this ApplicationModulesManager manager) {
            return manager.CheckBlazor(typeof(BlazorStartup).FullName, typeof(BlazorModule).Namespace).ToUnit();
        }

    }
}
