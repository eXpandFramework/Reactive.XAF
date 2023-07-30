using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ViewExtensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class WindowExtensions{
        public static IObservable<Window> ViewControllersActivated(this IObservable<Window> source) 
            => source.SelectMany(item => item.WhenEvent(nameof(Window.ViewControllersActivated))
                .TakeUntil(item.WhenDisposedFrame()).Select(_ => item));

        public static IObservable<Window> WhenIsLookupPopup(this IObservable<Window> source) 
            => source.Where(controller => controller.Template is ILookupPopupFrameTemplate);
        
        public static IObservable<ViewItem> ViewItems(this Window frame,params Type[] objectTypes) 
            => frame.NestedFrameContainers(objectTypes).OfType<ViewItem>();

        public static IObservable<IFrameContainer> NestedFrameContainers<TWindow>(this TWindow window,
            params Type[] objectTypes) where TWindow : Window
            => window.View.ToCompositeView().NestedFrameContainers(objectTypes);
    }
}