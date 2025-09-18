using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Fasterflect;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.FaultHub;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ViewExtensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class ViewItemExtensions{
        [SuppressMessage("ReSharper", "NotAccessedField.Local")] private static readonly Type GridListEditorType;

        static ViewItemExtensions() => GridListEditorType = AppDomain.CurrentDomain.GetAssemblyType("DevExpress.ExpressApp.Win.Editors.GridListEditor");

        #region High-Level Logical Operations
        public static IObservable<TTabbedControl> WhenTabControl<TTabbedControl>(this XafApplication application, Type objectType=null, Func<DetailView, bool> match=null, Func<IModelTabbedGroup, bool> tabMatch=null) 
            => application.WhenDetailViewCreated(objectType).ToDetailView()
                .Where(view => match?.Invoke(view)??true)
                .SelectMany(detailView => detailView.WhenTabControl(tabMatch)).Cast<TTabbedControl>()
                .PushStackFrame();
        
        public static IObservable<TTabbedControl> WhenTabControl<TTabbedControl>(this IObservable<DashboardViewItem> source) 
            => source.SelectMany(item => item.Frame.View.ToDetailView().WhenTabControl()).Cast<TTabbedControl>().PushStackFrame();
        
        public static IObservable<(T viewItem, NestedFrame nestedFrame)> ToNestedFrames<T>(this IObservable<T> source, params Type[] nestedObjectTypes) where T:ViewItem 
            => source.Cast<IFrameContainer>().Where(container => container.Frame!=null)
                .Select(container => (viewItem: ((T) container),nestedFrame: ((NestedFrame) container.Frame)))
                .Where(t =>!nestedObjectTypes.Any()|| nestedObjectTypes.Any(type => type.IsAssignableFrom(t.nestedFrame.View.ObjectTypeInfo.Type))).PushStackFrame();

        public static IObservable<T> ControlCreated<T>(this IEnumerable<T> source) where T:ViewItem 
            => source.ToObservable(ImmediateScheduler.Instance).ControlCreated().PushStackFrame();
        #endregion

        #region Low-Level Plumbing
        public static IObservable<TView> ToView<TView>(this IObservable<DashboardViewItem> source)
            => source.Select(item => item.Frame.View).Cast<TView>();

        public static IObservable<T> WhenControlCreated<T>(this T source,bool emitExisting=false) where T:ViewItem 
            =>emitExisting&&source.Control!=null?source.Observe(): source.ProcessEvent(nameof(ViewItem.ControlCreated))
                .Select(_ => source).TakeUntilDisposed();

        public static IObservable<T> TakeUntilDisposed<T>(this IObservable<T> source) where T : ViewItem
            => source.TakeWhileInclusive(item => !item.IsDisposed());

        public static IObservable<TView> OfView<TView>(this IObservable<DashboardViewItem> source)
            => source.Select(item => item.Frame.View).OfType<TView>();
        
        public static IObservable<DashboardViewItem> When(this IObservable<DashboardViewItem> source, params ViewType[] viewTypes) 
            => source.Where(item => viewTypes.All(viewType => item.InnerView.Is(viewType)));

        public static bool IsDisposed<T>(this T source) where T : ViewItem
            => (bool)source.GetPropertyValue("IsDisposed");
        
        public static IObservable<T> ControlCreated<T>(this IObservable<T> source) where T:ViewItem
            => source.SelectMany(item => item.WhenControlCreated());
        #endregion
    }
}