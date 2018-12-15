using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;

namespace DevExpress.XAF.Modules.Reactive.Services{
    public static class WindowExtensions{
        public static IObservable<Window> ViewControllersActivated(this IObservable<Window> source){
            return source.SelectMany(item => {
                return Observable.FromEventPattern<EventHandler, EventArgs>(
                    handler => item.ViewControllersActivated += handler,
                    handler => item.ViewControllersActivated -= handler).Select(pattern => item);
            });
        }

        public static IObservable<Window> WhenIsLookupPopup(this IObservable<Window> source) {
            return source.Where(controller => controller.Template is ILookupPopupFrameTemplate);
        }
    }
}