using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Services.Controllers{
    public static partial class ControllerExtensions{
        public static IObservable<T> TakeUntilDeactivated<T>(this IObservable<T> source,Controller controller) => source.TakeUntil(controller.WhenDeactivated());

        public static IObservable<TController> WhenIsOnLookupPopupFrame<TController>(this IObservable<TController> source) where TController : Controller =>
	        source.Where(controller => controller.Frame.Template is ILookupPopupFrameTemplate)
		        .Cast<TController>();

        public static IObservable<TController> When<TController>(this TController source, ActionBase actionBase) where TController : Controller =>
	        Observable.Return(source).When(actionBase);

        public static IObservable<TController> When<TController>(this IObservable<TController> source,ActionBase actionBase) where TController : Controller => source
	        .When(actionBase.TargetViewType, actionBase.TargetObjectType??typeof(object), actionBase.TargetViewNesting);

        public static IObservable<TController> When<TController>(this IObservable<TController> source,ViewType viewType=ViewType.Any,Type objectType=null,Nesting nesting=Nesting.Any) where TController:Controller{
            objectType ??= typeof(object);            
            return source
                .SelectMany(controller => {
                    if (controller.Frame?.View != null )
                        return Observable.Return(controller)
                            .Where(_ => _.Frame.View.Fits(viewType, nesting) && objectType.IsAssignableFrom(_.Frame.View.ObjectTypeInfo.Type));
                    return Observable.Return(controller)
                        .FrameAssigned()
                        .SelectMany(_ => _.Frame.WhenViewChanged().Select(tuple => _))
                        ;
                }).Where(_ => _.Frame.View.Fits(viewType, nesting) && objectType.IsAssignableFrom(_.Frame.View.ObjectTypeInfo.Type));
        }


        public static IObservable<T> ViewControlsCreated<T>(this IObservable<T> controllers) where T:ViewController =>
	        controllers.SelectMany(controller => {
		        return Observable.FromEventPattern<EventHandler, EventArgs>(
				        handler => controller.ViewControlsCreated += handler,
				        handler => controller.ViewControlsCreated -= handler,ImmediateScheduler.Instance)
			        .Select(pattern => controller);
	        });

        public static T As<T>(this Controller controller) where T : Controller{
	        return controller as T;
        }

        public static IObservable<T> WhenViewControlsCreated<T>(this T controller) where T : ViewController{
	        return controller.WhenActivated(true)
		        .SelectMany(viewController => viewController.View.WhenControlsCreated().To(viewController));
        }

        public static IObservable<T> WhenActivated<T>(this T controller,bool emitWhenActive=false) where T : Controller => controller.ReturnObservable().Activated(emitWhenActive);

        public static IObservable<T> Activated<T>(this IObservable<T> controllers,bool emitWhenActive=false) where T:Controller =>
	        controllers.SelectMany(controller => emitWhenActive && controller.Active ? controller.ReturnObservable()
		        : Observable.FromEventPattern<EventHandler, EventArgs>(
				        handler => controller.Activated += handler,
				        handler => controller.Activated -= handler, ImmediateScheduler.Instance)
			        .Select(pattern => controller));

        public static IObservable<T> WhenDeactivated<T>(this T controller) where T : Controller =>
	        Observable.FromEventPattern<EventHandler, EventArgs>(
			        handler => controller.Deactivated += handler,
			        handler => controller.Deactivated -= handler,ImmediateScheduler.Instance)
		        .Select(pattern => (T) pattern.Sender).TakeUntilDisposed(controller);

        public static IObservable<T> Deactivated<T>(this IObservable<T> controllers) where T:Controller =>
	        controllers.SelectMany(controller => Observable.FromEventPattern<EventHandler, EventArgs>(
			        handler => controller.Deactivated += handler,
			        handler => controller.Deactivated -= handler,ImmediateScheduler.Instance)
		        .Select(pattern => controller));

        public static IObservable<T> FrameAssigned<T>(this IObservable<T> controllers,TemplateContext templateContext=default) where T:Controller =>
	        controllers.Select(controller => {
			        var frameAssigned = Observable.FromEventPattern<EventHandler, EventArgs>(
					        handler => controller.FrameAssigned += handler,
					        handler => controller.FrameAssigned -= handler,ImmediateScheduler.Instance)
				        .Select(pattern => controller);
			        return controller.Frame!=null ? frameAssigned.StartWith(controller) : frameAssigned;
		        })
		        .Concat()
		        .Where(arg => templateContext == default || arg.Frame.Context == templateContext)
		        .Select(arg => arg);
    }
}