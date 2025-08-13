using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ViewExtensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class WindowExtensions{
        #region High-Level Logical Operations
        public static IObservable<ViewItem> ViewItems(this Window frame,params Type[] objectTypes) 
            => frame.NestedFrameContainers(objectTypes).OfType<ViewItem>().PushStackFrame();

        public static IObservable<IFrameContainer> NestedFrameContainers<TWindow>(this TWindow window,
            params Type[] objectTypes) where TWindow : Window
            => window.View.ToCompositeView().NestedFrameContainers(objectTypes).PushStackFrame();
        #endregion

        #region Low-Level Plumbing
        public static IObservable<Window> ViewControllersActivated(this IObservable<Window> source) 
            => source.SelectMany(item => item.ProcessEvent(nameof(Window.ViewControllersActivated))
                .TakeUntil(item.WhenDisposedFrame()).Select(_ => item));

        public static IObservable<Window> WhenIsLookupPopup(this IObservable<Window> source) 
            => source.Where(controller => controller.Template is ILookupPopupFrameTemplate);
        #endregion
    }
}