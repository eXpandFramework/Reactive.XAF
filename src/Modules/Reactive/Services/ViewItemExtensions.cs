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
        public static IObservable<(T viewItem, NestedFrame nestedFrame)> ToNestedFrames<T>(this IObservable<T> source, params Type[] nestedObjectTypes) where T:ViewItem 
            => source.Cast<IFrameContainer>().Where(container => container.Frame!=null)
                .Select(container => (viewItem: ((T) container),nestedFrame: ((NestedFrame) container.Frame)))
                .Where(_ =>!nestedObjectTypes.Any()|| nestedObjectTypes.Any(type => type.IsAssignableFrom(_.nestedFrame.View.ObjectTypeInfo.Type)));

        public static IObservable<T> ControlCreated<T>(this IEnumerable<T> source) where T:ViewItem 
            => source.ToObservable(ImmediateScheduler.Instance).ControlCreated();

        public static IObservable<T> WhenControlCreated<T>(this T source) where T:ViewItem 
            => source.ReturnObservable().ControlCreated();

        public static IObservable<T> ControlCreated<T>(this IObservable<T> source) where T:ViewItem
            => source.SelectMany(item => item.WhenEvent(nameof(ViewItem.ControlCreated))
                .Select(_ => item));
    }
}