using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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

        // static readonly ISubject<object[]> CommitSignal = Subject.Synchronize(new Subject<object[]>());
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
                    .ToFirst()
                    .RefreshView());
            
        // private static IObservable<View> RefreshObjectViewWhenCommitted<TObject>(this XafApplication application, Type detailViewObjectType=null,Func<Frame,TObject[],bool> match=null) where TObject : class
        //     => application.WhenFrame(detailViewObjectType, detailViewObjectType != null ? ViewType.DetailView : ViewType.ListView).Where(frame => frame.View.IsRoot)
        //         .SelectMany(frame => frame.WhenCommit(match)
        //             .RepeatWhen(observable => Observable.Defer(() => observable.WhenNotDefault(_ => frame.View)
        //                 .TakeUntil(application.WhenNestedObjectSpaceCreated( frame.View.ObjectSpace)))
        //                 .RepeatWhen(_ => application.WhenNestedObjectSpaceCreated( frame.View.ObjectSpace)
        //                     .SelectMany(space => space.WhenDisposed()))
        //                 .SelectMany(_ => frame.WhenObjectSpaceNotModified()
        //                     .RefreshView(frame)))
        //         )
        //         .Merge(application.WhenProviderCommittedDetailed<TObject>(ObjectModification.All,emitUpdatingObjectSpace:true,_ => true)
        //             .ToObjectsGroup().Do(CommitSignal.OnNext).IgnoreElements().To<View>())
        //         .TakeUntilDisposed(application);

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
        
        // private static IObservable<View> WhenObjectSpaceNotModified(this Frame frame) 
        //     => frame.View.ObjectSpace.WhenModifyChanged().To(frame.View).StartWith(frame.View)
        //         .TakeUntil(frame.View.ObjectSpace.WhenDisposed()).Where(_ => !frame.View.ObjectSpace.IsModified)
        //         .Delay(100.Milliseconds()).ObserveOnContext();

        // private static IObservable<View> WhenCommit<TObject>(this Frame frame,Func<Frame, TObject[], bool> match) where TObject : class 
        //     => Observable.Defer(() => CommitSignal.OfType<TObject[]>().TakeUntil(frame.View.ObjectSpace.WhenDisposed()
        //             .Merge(frame.WhenDisposedFrame()).Take(1))
        //         .Quiescent(2.Seconds()).WhenNotEmpty()
        //         .ObserveOnContext().SelectMany().Where(arg => match?.Invoke(frame, arg) ?? true).To(frame.View).WhenNotDefault(view => view?.ObjectSpace)
        //         .Take(1));
    }
}