using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Layout;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class LayoutManagerExtensions{
        public static IObservable<EventPattern<CustomizeAppearanceEventArgs>> WhenCustomizeAppearence(this LayoutManager layoutManager){
            var manager = ((ISupportAppearanceCustomization) layoutManager);
            return Observable.FromEventPattern<EventHandler<CustomizeAppearanceEventArgs>, CustomizeAppearanceEventArgs>(
                    h => manager.CustomizeAppearance += h,
                    h => manager.CustomizeAppearance -= h,ImmediateScheduler.Instance);
            
        }
    }
}