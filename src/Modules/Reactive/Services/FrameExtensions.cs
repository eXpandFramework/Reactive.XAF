using System;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class NestedFrameExtensions{
        public static IObservable<TFrame> WhenIsNotOnLookupPopupTemplate<TFrame>(this IObservable<TFrame> source)
            where TFrame : Frame{

            return source.Where(frame => !(frame.Template is ILookupPopupFrameTemplate))
                .Cast<TFrame>();

        }

        public static IObservable<TFrame> WhenIsOnLookupPopupTemplate<TFrame>(this IObservable<TFrame> source) where TFrame : Frame{
            return source.Where(frame => frame.Template is ILookupPopupFrameTemplate)
                .Cast<TFrame>();
        }

    }
    public static class FrameExtensions{
        public static IObservable<TFrame> WhenModule<TFrame>(
            this IObservable<TFrame> source, Type moduleType) where TFrame:Frame{
            return source
                .Where(_ => _.Application.Modules.FindModule(moduleType)!=null);
        }

        public static IObservable<TFrame> When<TFrame>(this IObservable<TFrame> source,TemplateContext templateContext) where TFrame:Frame{
            return source.Where(window => window.Context == templateContext);
        }

        public static IObservable<T> TakeUntilDisposingMainWindow<T>(this IObservable<T> source){
//            return source;
            var disposing = RxApp.Windows.When(TemplateContext.ApplicationWindow).DisposingFrame().ToUnit()
                .Merge(RxApp.Application.Disposed().Select(tuple => tuple).ToUnit())
                .FirstAsync()
                ;
            return source.TakeUntil(disposing);
        }

        public static IObservable<T> When<T>(this IObservable<T> source, Frame parentFrame,NestedFrame nestedFrame){
            return source
                .Where(_ => nestedFrame?.View!=null&&parentFrame?.View!=null)
                .TakeUntil(parentFrame.View.WhenCurrentObjectChanged())
                .TakeUntil(nestedFrame.WhenDisposingFrame().Merge(parentFrame.WhenDisposingFrame()));
        }

        internal static IObservable<TFrame> WhenFits<TFrame>(this IObservable<TFrame> source, ActionBase  action) where TFrame:Frame{
            return source.WhenFits(action.TargetViewType, action.TargetObjectType);
        }

        internal static IObservable<TFrame> WhenFits<TFrame>(this IObservable<TFrame> source,ViewType viewType,Type objectType=null,Nesting nesting=Nesting.Any,bool? isPopupLookup=null) where TFrame:Frame{
            return source.SelectMany(_ => _.View != null ? _.AsObservable() : _.WhenViewChanged().Select(tuple => _))
                .Where(frame => frame.View.Fits(viewType, nesting, objectType))
                .Where(_ => {
                    if (isPopupLookup.HasValue){
                        var ispopupLookupTemplate = _.Template is ILookupPopupFrameTemplate;
                        return isPopupLookup.Value ? ispopupLookupTemplate : !ispopupLookupTemplate;
                    }
                    return true;
                });
        }

        public static IObservable<(TFrame frame, ViewChangedEventArgs args)> WhenViewChanged<TFrame>(
            this TFrame source) where TFrame : Frame{
            return Observable.Return(source).ViewChanged();
        }

        public static IObservable<(TFrame frame, ViewChangedEventArgs args)> ViewChanged<TFrame>(this IObservable<TFrame> source) where TFrame:Frame{
            return source
                .SelectMany(item => Observable.FromEventPattern<EventHandler<ViewChangedEventArgs>, ViewChangedEventArgs>(h => item.ViewChanged += h, h => item.ViewChanged -= h))
                .Select(pattern => pattern)
                .TransformPattern<ViewChangedEventArgs,TFrame>();
        }

        public static IObservable<T> TemplateChanged<T>(this IObservable<T> source,bool skipWindowsCtorAssigment=false) where T:Frame{
            var winWindowCreated = RxApp.Windows.Where(window => window.GetType().Name=="WinWindow").Cast<T>()
                .Select(frame => frame);
            return source.SelectMany(item => {
                return Observable.FromEventPattern<EventHandler, EventArgs>(
                        handler => item.TemplateChanged += handler,
                        handler => item.TemplateChanged -= handler)
                    .Select(pattern => item)
                    .TakeUntil(item.WhenDisposingFrame());
            })
            .Merge(winWindowCreated);
        }

        public static IObservable<TFrame> WhenTemplateChanged<TFrame>(this TFrame source) where TFrame : Frame{
            return Observable.Return(source).TemplateChanged();
        }

        public static IObservable<TFrame> WhenTemplateViewChanged<TFrame>(this TFrame source) where TFrame : Frame{
            return Observable.Return(source).TemplateViewChanged();
        }

        public static IObservable<T> TemplateViewChanged<T>(this IObservable<T> source) where T:Frame{
            return source.SelectMany(item => {
                return Observable.FromEventPattern<EventHandler, EventArgs>(
                    handler => item.TemplateViewChanged += handler,
                    handler => item.TemplateViewChanged -= handler).Select(pattern => item)
                    .TakeUntil(item.WhenDisposingFrame())
                    ;
            });
        }

        public static IObservable<Unit> WhenDisposingFrame<TFrame>(this TFrame source) where TFrame:Frame{
            return DisposingFrame(Observable.Return(source));
        }

        public static IObservable<Unit> DisposingFrame<TFrame>(this IObservable<TFrame> source) where TFrame:Frame{
            return source.SelectMany(async item => {
                return await Observable.StartAsync(async () => await Observable.FromEventPattern<EventHandler, EventArgs>(
                    handler => item.Disposing += handler,
                    handler => item.Disposing -= handler).ToUnit());
            });
        }
    }
}