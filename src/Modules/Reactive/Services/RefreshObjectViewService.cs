using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.FrameExtensions;

namespace Xpand.XAF.Modules.Reactive.Services {
    public static class RefreshObjectViewService {
        public static IObservable<View> RefreshListViewWhenObjectCommitted<TObject>(this XafApplication application) where TObject : class
            => application.RefreshObjectViewWhenCommitted<TObject>();
        
        public static IObservable<View> RefreshDetailViewWhenObjectCommitted<TObject>(this XafApplication application, Type detailViewObjectType,Func<Frame,TObject[],bool> match=null) where TObject : class 
            => application.RefreshObjectViewWhenCommitted(detailViewObjectType, match);

        
        private static IObservable<View> RefreshObjectViewWhenCommitted<TObject>(this XafApplication application, Type detailViewObjectType=null,Func<Frame,TObject[],bool> match=null) where TObject : class
            =>application.WhenProviderCommittedDetailed<TObject>(ObjectModification.All,emitUpdatingObjectSpace:true,_ => true).ToObjects()
                .BufferUntilInactive(2.Seconds()).Publish(source => source.RefreshObjectViewWhenCommitted(application,detailViewObjectType,match));

        private static IObservable<View> RefreshObjectViewWhenCommitted<TObject>(
            this IObservable<IList<TObject>> source, XafApplication application, Type detailViewObjectType = null,
            Func<Frame, TObject[], bool> match = null) where TObject : class
            => application.WhenFrame(detailViewObjectType, detailViewObjectType != null ? ViewType.DetailView : ViewType.ListView)
                .Where(frame => frame.View.IsRoot)
                .SelectMany(frame => source.ObserveOnContext().TakeUntil(_ => frame.IsDisposed()).Where(list => match?.Invoke(frame,list.ToArray())??true).To(frame.View)
                    .WithLatestFrom(frame.View.ObjectSpace.WhenModifyChanged().StartWith(frame.View.ObjectSpace),(view, space) => (view, space)).Where(t => !t.space.IsModified)
                    .ToFirst().Where(view => !view.IsDisposed)
                    .RefreshView());

        private static IObservable<View> RefreshView(this IObservable<View> source)
            => source.DoSafe(view => {
                if (view is DetailView detailView) {
                    detailView.GetItems<DashboardViewItem>()
                        .Select(item => item.InnerView?.ObjectSpace).WhereNotDefault()
                        .Do(objectSpace => objectSpace.Refresh())
                        .Enumerate();
                }
                view.ObjectSpace.Refresh();
            });
        
    }
}