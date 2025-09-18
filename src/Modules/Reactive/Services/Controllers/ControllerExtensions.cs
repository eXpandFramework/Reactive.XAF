using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Relay;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Services.Controllers{
    public static partial class ControllerExtensions {
        public static IObservable<Frame> WhenFrame(this Controller controller)
            => controller.WhenFrameAssigned().ViewChanged().Select(_ => controller.Frame)
                .PushStackFrame();
        public static IObservable<T> TakeUntilDeactivated<T>(this IObservable<T> source, Controller controller) 
            => source;
            // => source.TakeUntil(controller.WhenDeactivated());

        public static IObservable<TController> WhenIsOnLookupPopupFrame<TController>(
            this IObservable<TController> source) where TController : Controller 
            => source.Where(controller => controller.Frame.Template is ILookupPopupFrameTemplate)
                .Cast<TController>();

        public static IObservable<TController> When<TController>(this TController source, ActionBase actionBase) where TController : Controller 
            => source.Observe().When(actionBase);

        public static IObservable<TController> When<TController>(this IObservable<TController> source,
            ActionBase actionBase) where TController : Controller => source
            .When(actionBase.TargetViewType, actionBase.TargetObjectType ?? typeof(object),
                actionBase.TargetViewNesting);

        public static IObservable<TController> When<TController>(this IObservable<TController> source,
            ViewType viewType = ViewType.Any, Type objectType = null, Nesting nesting = Nesting.Any)
            where TController : Controller{
            objectType ??= typeof(object);
            return source
                .SelectMany(controller => controller.Frame?.View != null ? controller.Observe()
                        .Where(c => c.Frame.View.Is(viewType, nesting) && objectType.IsAssignableFrom(c.Frame.View.ObjectTypeInfo.Type))
                    : Observable.Return(controller).FrameAssigned().SelectMany(c => c.Frame.WhenViewChanged().Select(_ => c)))
                .Where(controller => controller.Frame.View.Is(viewType, nesting) && objectType.IsAssignableFrom(controller.Frame.View.ObjectTypeInfo.Type))
                .PushStackFrame();
        }


        public static IObservable<T> ViewControlsCreated<T>(this IObservable<T> controllers) where T : ViewController 
            => controllers.SelectMany(controller => controller.WhenViewControlsCreated());

        public static T As<T>(this Controller controller) where T : Controller 
            => controller as T;

        public static IObservable<T> WhenViewControlsCreated<T>(this T controller) where T : ViewController 
            => controller.ProcessEvent(nameof(ViewController.ViewControlsCreated))
                .PushStackFrame();

        public static IObservable<T> WhenActivated<T>(this T controller, bool emitWhenActive = false) where T : Controller 
            => controller.Observe().Activated(emitWhenActive);
        
        public static IObservable<T> WhenActivated<T>(this IObservable<T> source) where T : Controller 
            => source.SelectMany(controller => controller.WhenActivated());
        
        
        public static IObservable<T2> SelectManyUntilDeactivated<T,T2>(this IObservable<T> source,Func<T,IObservable<T2>> selector) where T:Controller
            => source.SelectMany(controller => selector(controller).TakeUntilDeactivated(controller))
                .PushStackFrame();
        
        public static IObservable<Frame> WhenFrameAssigned(this Controller controller)
            => controller.ProcessEvent(nameof(Controller.FrameAssigned)).Select(_ => controller.Frame)
                .TakeUntilDisposed(controller)
                .PushStackFrame();

        public static IObservable<T> Activated<T>(this IObservable<T> controllers, bool emitWhenActive = false) where T : Controller 
            => controllers.SelectMany(controller => emitWhenActive && controller.Active
                ? controller.Observe()
                : controller.ProcessEvent(nameof(Controller.Activated)).TakeUntilDisposed(controller)
                    .Select(_ => controller))
                .TraceRX(controller => controller.Name)
                .PushStackFrame();

        public static IObservable<T> WhenDeactivated<T>(this T controller) where T : Controller => controller.ProcessEvent(nameof(Controller.Deactivated)).To(controller).TakeUntilDisposed(controller);

        public static IObservable<T> Deactivated<T>(this IObservable<T> controllers) where T : Controller 
            => controllers.SelectMany(controller => controller.WhenDeactivated());

        public static IObservable<T> FrameAssigned<T>(this IObservable<T> controllers, TemplateContext templateContext = default) where T : Controller 
            => controllers.Select(controller => {
                    var frameAssigned = controller.WhenFrameAssigned().To(controller);
                    return controller.Frame != null ? frameAssigned.StartWith(controller) : frameAssigned;
                })
                .Concat()
                .Where(arg => templateContext == default || arg.Frame.Context == templateContext)
                .Select(arg => arg)
                .PushStackFrame();
    }
}