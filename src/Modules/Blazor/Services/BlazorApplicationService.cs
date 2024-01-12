using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Model;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Blazor.Services {
    public static class BlazorApplicationService {
        public static IObservable<DxFormLayoutTabPagesModel> WhenTabControl(this BlazorApplication application,
            Type objectType, Func<DetailView, bool> match = null, Func<IModelTabbedGroup, bool> tabMatch = null)
            => application.WhenTabControl<DxFormLayoutTabPagesModel>(objectType);
    }
}