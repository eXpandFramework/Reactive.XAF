using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Utils;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Xpo;
using Fasterflect;

using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.SequenceGenerator{
    public static class Extensions{
        static readonly ImmediateScheduler EventsScheduler = Scheduler.Immediate;


        internal static IObservable<(IObjectSpace objectSpace, CancelEventArgs e)> WhenRollingBack(this IObjectSpace objectSpace) 
            => Observable.FromEventPattern<EventHandler<CancelEventArgs>, CancelEventArgs>(
                    h => objectSpace.RollingBack += h, h => objectSpace.RollingBack -= h, EventsScheduler)
                .TransformPattern<CancelEventArgs, IObjectSpace>();

        internal static IObservable<IObjectSpace> WhenObjectSpaceCreated(this XafApplication application,bool includeNonPersistent=false) 
            => Observable.FromEventPattern<EventHandler<ObjectSpaceCreatedEventArgs>, ObjectSpaceCreatedEventArgs>(
                    h => application.ObjectSpaceCreated += h, h => application.ObjectSpaceCreated -= h, EventsScheduler)
                .Select(_ => _.EventArgs.ObjectSpace).Where(space => includeNonPersistent || !(space is NonPersistentObjectSpace));


        internal static IObservable<EventPattern<EventArgs>> WhenAfterCommitTransaction(this Session session) 
            => Observable.FromEventPattern<SessionManipulationEventHandler, EventArgs>(h => session.AfterCommitTransaction += h, h => session.AfterCommitTransaction -= h, EventsScheduler)
                .TakeUntil(session.WhenDisposed());


        internal static IObservable<ObjectManipulationEventArgs> WhenObjectSaving(this Session session) 
            => Observable.FromEventPattern<ObjectManipulationEventHandler, EventArgs>(h => session.ObjectSaving += h, h => session.ObjectSaving -= h, EventsScheduler)
                .Select(pattern => pattern.EventArgs).Cast<ObjectManipulationEventArgs>()
                .TakeUntil(session.WhenDisposed());


        internal static IObservable<Session> WhenObjectsSaved(this Session session) 
            => Observable.FromEventPattern<ObjectsManipulationEventHandler, EventArgs>(h => session.ObjectsSaved += h, h => session.ObjectsSaved -= h, EventsScheduler)
                .Select(pattern => pattern.Sender).Cast<Session>()
                .TakeUntil(session.WhenDisposed());

        internal static IObservable<EventPattern<EventArgs>> WhenAfterRollbackTransaction(this Session session) 
            => Observable.FromEventPattern<SessionManipulationEventHandler, EventArgs>(h => session.AfterRollbackTransaction += h, h => session.AfterRollbackTransaction -= h, EventsScheduler);


        internal static IObservable<EventPattern<EventArgs>> WhenFailedCommitTransaction(this Session session) 
            => Observable.FromEventPattern<SessionOperationFailEventHandler, EventArgs>(h => session.FailedCommitTransaction += h, h => session.FailedCommitTransaction -= h, EventsScheduler);

        internal static string GetConnectionString(this IObjectSpaceProvider spaceProvider){
            Guard.TypeArgumentIs(typeof(XPObjectSpaceProvider),spaceProvider.GetType(),nameof(spaceProvider));
            var objectSpaceProvider = ((XPObjectSpaceProvider) spaceProvider);
            return objectSpaceProvider.DataLayer?.Connection != null ? objectSpaceProvider.DataLayer.Connection.ConnectionString
                : ((IXpoDataStoreProvider) objectSpaceProvider.GetPropertyValue("DataStoreProvider")).ConnectionString;
        }
    }
}