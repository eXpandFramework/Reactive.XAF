using System;
using DevExpress.Xpo;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.XAF.Xpo.SessionExtensions {
    public static partial class SessionExtensions {
        public static IObservable<ObjectManipulationEventArgs> WhenObjectSaving(this Session session) 
            => session.ProcessEvent<ObjectManipulationEventArgs>(nameof(Session.ObjectSaving)).TakeUntilDisposed(session);

        public static IObservable<TSession> WhenAfterRollbackTransaction<TSession>(this TSession session) where TSession:Session 
            => session.ProcessEvent(nameof(Session.AfterRollbackTransaction)).To(session).TakeUntilDisposed(session);

        public static IObservable<TSession> WhenFailedCommitTransaction<TSession>(this TSession session) where TSession:Session 
            => session.ProcessEvent(nameof(Session.FailedCommitTransaction)).To(session).TakeUntilDisposed(session);
        
        public static IObservable<TSession> WhenAfterCommitTransaction<TSession>(this TSession session) where TSession : Session
            => session.ProcessEvent(nameof(Session.AfterCommitTransaction)).TakeUntilDisposed(session).To(session);
        
        public static IObservable<TSession> WhenObjectsSaved<TSession>(this TSession session) where TSession:Session 
            => session.ProcessEvent(nameof(Session.ObjectsSaved)).To(session).TakeUntilDisposed(session);
    }
}