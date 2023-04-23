using System;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Utils;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Xpo;
using Fasterflect;

using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.SequenceGenerator{
    public static class Extensions{
        internal static IObservable<EventPattern<EventArgs>> WhenAfterCommitTransaction(this Session session) 
            => session.WhenEvent(nameof(Session.AfterCommitTransaction))
                .Select(pattern => new EventPattern<EventArgs>(pattern, EventArgs.Empty))
                .TakeUntil(session.WhenDisposed());


        internal static IObservable<ObjectManipulationEventArgs> WhenObjectSaving(this Session session) 
            => session.WhenEvent<ObjectManipulationEventArgs>(nameof(Session.ObjectSaving))
                .TakeUntil(session.WhenDisposed());


        internal static IObservable<Session> WhenObjectsSaved(this Session session) 
            => session.WhenEvent(nameof(Session.ObjectsSaved)).To(session)
                .TakeUntil(session.WhenDisposed());

        internal static IObservable<EventPattern<EventArgs>> WhenAfterRollbackTransaction(this Session session) 
            => session.WhenEvent(nameof(Session.AfterRollbackTransaction)).Select(pattern => new EventPattern<EventArgs>(pattern, EventArgs.Empty));


        internal static IObservable<EventPattern<EventArgs>> WhenFailedCommitTransaction(this Session session) 
            => session.WhenEvent(nameof(Session.FailedCommitTransaction)).Select(pattern => new EventPattern<EventArgs>(pattern, EventArgs.Empty));

        internal static string GetConnectionString(this IObjectSpaceProvider spaceProvider){
            Guard.TypeArgumentIs(typeof(XPObjectSpaceProvider),spaceProvider.GetType(),nameof(spaceProvider));
            var objectSpaceProvider = ((XPObjectSpaceProvider) spaceProvider);
            return objectSpaceProvider.DataLayer?.Connection != null ? objectSpaceProvider.DataLayer.Connection.ConnectionString
                : ((IXpoDataStoreProvider) objectSpaceProvider.GetPropertyValue("DataStoreProvider")).ConnectionString;
        }
    }
}