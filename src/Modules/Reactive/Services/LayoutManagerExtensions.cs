using System;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Layout;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class LayoutManagerExtensions{
        public static IObservable<CustomizeAppearanceEventArgs> WhenCustomizeAppearance(this LayoutManager layoutManager) 
            => layoutManager.WhenEvent<CustomizeAppearanceEventArgs>(nameof(ISupportAppearanceCustomization.CustomizeAppearance));
    }
}