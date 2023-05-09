using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class WindowExtensions{
        public static IObservable<Window> ViewControllersActivated(this IObservable<Window> source) 
            => source.SelectMany(item => item.WhenEvent(nameof(Window.ViewControllersActivated)).Select(_ => item));

        public static IObservable<Window> WhenIsLookupPopup(this IObservable<Window> source) 
            => source.Where(controller => controller.Template is ILookupPopupFrameTemplate);
    }
}