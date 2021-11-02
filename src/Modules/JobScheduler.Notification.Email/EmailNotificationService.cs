using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.BusinessObjects;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.Email {
    public static class EmailNotificationService {
        private static readonly ISubject<(ObjectStateNotification job, object[] objects)> EmailNotificationSubject =
            Subject.Synchronize(new Subject<(ObjectStateNotification job, object[] objects)>());
        
        public static IObservable<Unit> Connect(this ApplicationModulesManager manager) 
            => manager.WhenApplication(application => application.WhenNotification()).ToUnit();

        public static IObservable<(ObjectStateNotification job, T[] objects)> WhenEmailNotification<T>(this XafApplication application) 
            => application.WhenEmailNotification(typeof(T)).Select(t => (t.job,t.objects.Cast<T>().ToArray())).AsObservable();
        
        public static IObservable<(ObjectStateNotification job, object[] objects)> WhenEmailNotification(this XafApplication application,Type objectType=null) {
            objectType ??= typeof(object);
            return EmailNotificationSubject.Select(t =>(t.job,t.objects.Where(o => objectType.IsInstanceOfType(o)).ToArray()) )
                .AsObservable().TraceEmailNotificationModule(t => t.job.Object.Name);
        }

        internal static IObservable<TSource> TraceEmailNotificationModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, EmailNotificationModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);
    }
}