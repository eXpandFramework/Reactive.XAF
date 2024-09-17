// using System;
// using System.Reactive;
// using System.Reactive.Linq;
// using System.Reactive.Subjects;
// using System.Threading;
// using System.Windows.Forms;
// using DevExpress.ExpressApp;
// using DevExpress.ExpressApp.MultiTenancy;
// using DevExpress.ExpressApp.SystemModule;
// using DevExpress.ExpressApp.Win;
// using DevExpress.Persistent.Base;
// using DevExpress.Persistent.Validation;
// using Xpand.Extensions.ExceptionExtensions;
// using Xpand.Extensions.Reactive.Conditional;
// using Xpand.Extensions.Reactive.Transform;
// using Xpand.Extensions.Reactive.Utility;
// using Xpand.Extensions.XAF.XafApplicationExtensions;
// using Xpand.TestsLib.Common;
// using Xpand.XAF.Modules.Reactive.Services;
// using Xpand.XAF.Modules.Reactive.Services.Actions;
//
// namespace Xpand.TestsLib{
//     public static class TestExtensions{
//         public static IObservable<T> StartWinTest<T>(this WinApplication application, IObservable<T> test,
//             string user, string connectionString, LogContext logContext = default)  
//             => SynchronizationContext.Current.Observe()
//                 .DoWhen(context => context is not WindowsFormsSynchronizationContext,_ => SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext()))
//                 .SelectMany(_ => application.Start(test, SynchronizationContext.Current, connectionString,user,logContext)).FirstOrDefaultAsync();
//
//         private static IObservable<T> Start<T>(this WinApplication application, IObservable<T> test,
//             SynchronizationContext context, string connectionString, string user = null, LogContext logContext = default) 
//             => context.Observe().Do(SynchronizationContext.SetSynchronizationContext)
//                 .SelectMany(_ => application.Start(TestTracing.WhenError().ThrowTestException().DoOnError(_ => application.Terminate(context)).To<T>()
//                             .Merge(application.Observe().EnsureMultiTenantMainDatabase(connectionString)
//                                 // .DeleteModelDiffs<TDBContext>(_ => connectionString,user)
//                                 .SelectMany(_ => application.Start(test,user,context))
//                                 .LogError()))
//                 )
//                 .Log(logContext);
//
//         public static IObservable<XafApplication> EnsureMultiTenantMainDatabase(this IObservable<XafApplication> source, string connectionString=null) 
//             => source.SelectMany(application => application.GetService<ITenantProvider>() == null || application.TenantsExist(connectionString)
//                 ? application.Observe() : application.WhenLoggedOn("Admin").IgnoreElements().Merge(application.WhenMainWindowCreated()
//                     .SelectMany(window => window.GetController<LogoffController>().LogoffAction.Trigger(application.WhenLoggedOff()))
//                     .To(application)).Take(1));
//
//         private static IObservable<T> Start<T>(this WinApplication application, IObservable<T> test, string user, SynchronizationContext context) 
//             => application.WhenLoggedOn(user).Take(1).IgnoreElements().To<T>().Merge(test.DoOnComplete(() => application.Terminate(context))
//                     .Publish(obs => application.GetRequiredService<IValidator>().RuleSet.WhenEvent<ValidationCompletedEventArgs>(nameof(RuleSet.ValidationCompleted))
//                         .DoWhen(e => !e.Successful,e => e.Exception.ThrowCaptured()).To<T>().TakeUntilCompleted(obs)
//                         .Merge(obs)));
//
//         private static void Terminate(this XafApplication application, SynchronizationContext context){
//             Logger.Exit();
//             context.Post(_ => application.Exit(), null);
//         }
//         
//         public static IObservable<T> Start<T>(this WinApplication application, IObservable<T> test){
//             var exitSignal = new Subject<Unit>();
//             return test.Merge(application.Defer(() => application.Observe().Do(winApplication => winApplication.Start()).Do(_ => exitSignal.OnNext())
//                     .Select(winApplication => winApplication)
//                     .Finally(() => { })
//                     .Catch<XafApplication, Exception>(exception => {
//                         Tracing.Tracer.LogError(exception);
//                         return Observable.Empty<XafApplication>();
//                     }).IgnoreElements()
//                     .To<T>()))
//                 .TakeUntil(exitSignal);
//         }
//     }
// }