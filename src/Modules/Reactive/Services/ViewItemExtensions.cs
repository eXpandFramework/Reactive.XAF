using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class ListPropertyEditorExtensions{
        public static IObservable<ListPropertyEditor> FrameChanged(this IEnumerable<ListPropertyEditor> source){
            return source.ToObservable()
                .SelectMany(item => {
                    return Observable
                        .FromEventPattern<EventHandler<EventArgs>, EventArgs>(h => item.FrameChanged += h,
                            h => item.FrameChanged -= h,ImmediateScheduler.Instance)
                        .Select(pattern => item);
                });
        }

    }
    public static class ViewItemExtensions{
        public static IObservable<(ViewItem viewItem, NestedFrame nestedFrame)> ToNestedFrames(this IObservable<ViewItem> source, params Type[] nestedObjectTypes){
            return source.Cast<IFrameContainer>()
                .Where(container => container.Frame!=null)
                .Select(container => (viewItem: ((ViewItem) container),nestedFrame: ((NestedFrame) container.Frame)))
                .Where(_ =>!nestedObjectTypes.Any()|| nestedObjectTypes.Any(type => type.IsAssignableFrom(_.nestedFrame.View.ObjectTypeInfo.Type)));
        }

        public static IObservable<ViewItem> ControlCreated(this IEnumerable<ViewItem> source){
            return source.ToObservable().ControlCreated();
        }

        public static IObservable<ViewItem> WhenControlCreated(this ViewItem source){
            return Observable.Return(source).ControlCreated();
        }

        public static IObservable<ViewItem> ControlCreated(this IObservable<ViewItem> source){
            return source
                .SelectMany(item => {
                    return Observable
                        .FromEventPattern<EventHandler<EventArgs>, EventArgs>(h => item.ControlCreated += h,
                            h => item.ControlCreated -= h,ImmediateScheduler.Instance)
                        .Select(pattern => item);
                });
        }

    }
}