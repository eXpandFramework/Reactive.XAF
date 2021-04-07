using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class ViewItemExtensions{
        public static IObservable<(ViewItem viewItem, NestedFrame nestedFrame)> ToNestedFrames(this IObservable<ViewItem> source, params Type[] nestedObjectTypes) 
            => source.Cast<IFrameContainer>()
                .Where(container => container.Frame!=null)
                .Select(container => (viewItem: ((ViewItem) container),nestedFrame: ((NestedFrame) container.Frame)))
                .Where(_ =>!nestedObjectTypes.Any()|| nestedObjectTypes.Any(type => type.IsAssignableFrom(_.nestedFrame.View.ObjectTypeInfo.Type)));

        public static IObservable<ViewItem> ControlCreated(this IEnumerable<ViewItem> source) => 
            source.ToObservable(ImmediateScheduler.Instance).ControlCreated();

        public static IObservable<ViewItem> WhenControlCreated(this ViewItem source) 
            => source.ReturnObservable().ControlCreated();

        public static IObservable<ViewItem> ControlCreated(this IObservable<ViewItem> source) 
            => source.SelectMany(item => Observable
                .FromEventPattern<EventHandler<EventArgs>, EventArgs>(h => item.ControlCreated += h,
                    h => item.ControlCreated -= h,ImmediateScheduler.Instance)
                .Select(pattern => item));
    }
}