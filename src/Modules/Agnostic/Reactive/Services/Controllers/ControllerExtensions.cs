using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Services.Controllers{
    public static partial class ControllerExtensions{
        public static IObservable<TController> WhenIsOnLookupPopupFrame<TController>(this IObservable<TController> source) where TController : Controller{
            return source.Where(controller => controller.Frame.Template is ILookupPopupFrameTemplate)
                .Cast<TController>();
        }

        public static IObservable<TController> When<TController>(this TController source,
            ActionBase actionBase) where TController : Controller{
            return Observable.Return(source).When(actionBase);
        }

        public static IObservable<TController> When<TController>(this IObservable<TController> source,ActionBase actionBase) where TController : Controller{
            return source.When(actionBase.TargetViewType, actionBase.TargetObjectType??typeof(object), actionBase.TargetViewNesting);
        }

        public static IObservable<TController> When<TController>(this IObservable<TController> source,ViewType viewType=ViewType.Any,Type objectType=null,Nesting nesting=Nesting.Any) where TController:Controller{
            objectType = objectType ?? typeof(object);            
            return source
                .SelectMany(controller => {
                    if (controller.Frame?.View != null )
                        return Observable.Return(controller).Where(_ => _.Frame.View.Fits(viewType, nesting) &&
                                                                        objectType.IsAssignableFrom(_.Frame.View
                                                                            .ObjectTypeInfo.Type));
                    return Observable.Return(controller)
                        .FrameAssigned()
                        .SelectMany(_ => _.Frame.WhenViewChanged().Select(tuple => _))
                        ;
                }).Where(_ => _.Frame.View.Fits(viewType, nesting) &&
                              objectType.IsAssignableFrom(_.Frame.View.ObjectTypeInfo.Type));
        }

        public static IObservable<T> WhenViewControlsCreated<T>(this T controller) where T : Controller{
            return Observable.Return(controller).Activated();
        }

        public static IObservable<T> ViewControlsCreated<T>(this IObservable<T> controllers) where T:ViewController{
            return controllers.SelectMany(controller => {
                return Observable.FromEventPattern<EventHandler, EventArgs>(
                        handler => controller.ViewControlsCreated += handler,
                        handler => controller.ViewControlsCreated -= handler).Select(pattern => controller)
                    .TakeUntil(controller.WhenDeactivated());
            });
        }

        public static IObservable<T> WhenActivated<T>(this T controller) where T : Controller{
            return Observable.Return(controller).Activated();
        }

        public static IObservable<T> Activated<T>(this IObservable<T> controllers) where T:Controller{
            return controllers.Select(controller => {
                return Observable.FromEventPattern<EventHandler, EventArgs>(
                    handler => controller.Activated += handler,
                    handler => controller.Activated -= handler).Select(pattern => controller)
                    .TakeUntil(controller.WhenDeactivated());
            }).Concat();
        }

        public static IObservable<T> WhenDeactivated<T>(this T controller) where T : Controller{
            return Observable.Return(controller).Deactivated();
        }

        public static IObservable<T> Deactivated<T>(this IObservable<T> controllers) where T:Controller{
            return controllers.Select(controller => {
                return Observable.FromEventPattern<EventHandler, EventArgs>(
                    handler => controller.Deactivated += handler,
                    handler => controller.Deactivated -= handler).Select(pattern => controller);
            }).Concat();
        }

        public static IObservable<T> FrameAssigned<T>(this IObservable<T> controllers,TemplateContext templateContext=default) where T:Controller{
            return controllers.Select(controller => {
                    var frameAssigned = Observable.FromEventPattern<EventHandler, EventArgs>(
                            handler => controller.FrameAssigned += handler,
                            handler => controller.FrameAssigned -= handler)
                        .Select(pattern => controller);
                    return controller.Frame!=null ? frameAssigned.StartWith(controller) : frameAssigned;
                })
                .Concat()
                .Where(arg => templateContext == default || arg.Frame.Context == templateContext)
                .Select(arg => arg);
        }
    }
}