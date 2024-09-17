using System.Reactive.Linq;
using System.Reactive.Subjects;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.MultiTenancy;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Win;
using DevExpress.Map.Kml.Model;
using DevExpress.Persistent.Validation;
using Microsoft.EntityFrameworkCore;
using XAF.Testing.XAF;
using Point = System.Drawing.Point;

namespace XAF.Testing.Win.XAF{
    public static class TestExtensions{
        public static IObservable<T> StartWinTest<T, TDBContext>(this WinApplication application, IObservable<T> test,
            string user, string connectionString, LogContext logContext = default) where TDBContext : DbContext 
            => SynchronizationContext.Current.Observe()
                .DoWhen(context => context is not WindowsFormsSynchronizationContext,_ => SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext()))
                .SelectMany(_ => application.Start<T,TDBContext>(test, SynchronizationContext.Current, connectionString,user,logContext)).FirstOrDefaultAsync();

        private static IObservable<T> Start<T, TDBContext>(this WinApplication application, IObservable<T> test,
            SynchronizationContext context, string connectionString, string user = null, LogContext logContext = default) where TDBContext : DbContext 
            => context.Observe().Do(SynchronizationContext.SetSynchronizationContext)
                .SelectMany(_ => application.Start(Tracing.WhenError().ThrowTestException().DoOnError(_ => application.Terminate(context)).To<T>()
                            .Merge(application.Observe().EnsureMultiTenantMainDatabase(connectionString)
                                .DeleteModelDiffs<TDBContext>(_ => connectionString,user)
                                .SelectMany(_ => application.Start(test,user,context))
                                .LogError()))
                )
                .Log(logContext);

        public static IObservable<XafApplication> EnsureMultiTenantMainDatabase(this IObservable<XafApplication> source, string connectionString=null) 
            => source.SelectMany(application => application.GetService<ITenantProvider>() == null || application.TenantsExist(connectionString)
                ? application.Observe() : application.WhenLoggedOn("Admin").IgnoreElements().Merge(application.WhenMainWindowCreated()
                    .SelectMany(window => window.GetController<LogoffController>().LogoffAction.Trigger(application.WhenLogOff()))
                    .To(application)).Take(1));

        private static IObservable<T> Start<T>(this WinApplication application, IObservable<T> test, string user, SynchronizationContext context) 
            => application.WhenLoggedOn(user).Take(1).IgnoreElements().To<T>().Merge(test.DoOnComplete(() => application.Terminate(context))
                    .Publish(obs => application.GetRequiredService<IValidator>().RuleSet.WhenEvent<ValidationCompletedEventArgs>(nameof(RuleSet.ValidationCompleted))
                        .DoWhen(e => !e.Successful,e => e.Exception.ThrowCaptured()).To<T>().TakeUntilCompleted(obs)
                        .Merge(obs)));

        private static void Terminate(this XafApplication application, SynchronizationContext context){
            Logger.Exit();
            context.Post(_ => application.Exit(), null);
        }

        public static IObservable<Form> MoveToInactiveMonitor(this IObservable<Form> source) 
            => source.Do( form => form.Handle.UseInactiveMonitorBounds(bounds => {
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(bounds.Left, bounds.Top);    
            }));

        public static void ChangeStartupState(this WinApplication application,FormWindowState windowState,bool moveToInactiveMonitor=true) 
            => application.WhenFrameCreated(TemplateContext.ApplicationWindow)
                .TemplateChanged().Select(frame => frame.Template)
                .Cast<Form>()
                .If(_ => moveToInactiveMonitor,form => form.Observe().MoveToInactiveMonitor(),form => form.Observe())
                .Do(form => form.WindowState = windowState)
                .TakeUntilDisposed(application)
                .Subscribe();
        
        public static IObservable<T> Start<T>(this WinApplication application, IObservable<T> test){
            var exitSignal = new Subject<Unit>();
            return test.Merge(application.Defer(() => application.Observe().Do(winApplication => winApplication.Start()).Do(_ => exitSignal.OnNext())
                    .Select(winApplication => winApplication)
                    .Finally(() => { })
                    .Catch<XafApplication, Exception>(exception => {
                        DevExpress.Persistent.Base.Tracing.Tracer.LogError(exception);
                        return Observable.Empty<XafApplication>();
                    }).IgnoreElements()
                    .To<T>()))
                .TakeUntil(exitSignal);
        }
    }
}