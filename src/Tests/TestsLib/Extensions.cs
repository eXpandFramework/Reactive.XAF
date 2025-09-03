﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Win.Core;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using Fasterflect;
using HarmonyLib;
using Moq;
using Moq.Protected;
using Xpand.Extensions.ExceptionExtensions;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Windows;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.TestsLib {
    public static class Extensions {
        [SuppressMessage("ReSharper", "UnusedParameter.Global")]
        public static IObservable<ConfirmationDialogClosedEventArgs> WhenConfirmationDialogClosed(this Messaging messaging)
            => typeof(Messaging).ProcessEvent<ConfirmationDialogClosedEventArgs>(nameof(Messaging.ConfirmationDialogClosed));

        public static bool ShowDialog() => false;

        public static void PatchFormShowDialog(this XafApplication application) 
            => application.Patch(harmony => harmony.Patch(typeof(Form).Method(nameof(Form.ShowDialog), Type.EmptyTypes),
                new HarmonyMethod(typeof(Extensions), nameof(ShowDialog))));

        public static void MockMessaging(this Messaging messaging,Func<DialogResult> result) {
            var mock = new Mock<Messaging>(messaging.GetPropertyValue("Application"));
            mock.Protected()
                .Setup<DialogResult>("ShowCore", ItExpr.IsAny<string>(), ItExpr.IsAny<string>(),
                    ItExpr.IsAny<MessageBoxButtons>(), ItExpr.IsAny<MessageBoxIcon>())
                .Returns(result);
            WinApplication.Messaging=mock.Object;
        }

        public static IObservable<Unit> StartWinTest(this IObservable<WinApplication> source, Func<WinApplication,IObservable<Unit>> test,TimeSpan timeout) 
            => source.SelectMany(application => Observable.Using(() => application,winApplication => winApplication.StartWinTest(test(winApplication)
                .Timeout(timeout)))).FirstAsync();

        public static IObservable<T> StartWinTest<T>(this WinApplication application, IObservable<T> test, string user=null) 
            => SynchronizationContext.Current.Observe()
                .DoWhen(context => context is not WindowsFormsSynchronizationContext,_ => SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext()))
                .SelectMany(_ => application.Start(test, SynchronizationContext.Current,user)).FirstOrDefaultAsync();

        private static IObservable<T> Start<T>(this WinApplication application, IObservable<T> test, SynchronizationContext context, string user = null) 
            => application.Start( exit => TestTracing.WhenError().ThrowTestException()
                    .DoOnError(_ => application.Terminate(context)).To<T>()
                    .Merge(application.Observe()
                        .SelectMany(_ => test.Start(application, user,context))
                        .LogError()
                    ).Buffer(exit).Take(1).SelectMany())
            ;
        
        private static IObservable<T> Start<T>(this IObservable<T> test,WinApplication application, string user, SynchronizationContext context) 
            => (user==null?application.WhenLoggedOn().ToFirst(): application.WhenLoggedOn(user)).Take(1).IgnoreElements().To<T>()
                .Merge(test.DoOnComplete(() => application.Terminate(context)).Publish(obs => application.GetRequiredService<IValidator>().RuleSet
                    .ProcessEvent<ValidationCompletedEventArgs>(nameof(RuleSet.ValidationCompleted))
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
        
        public static IObservable<T> Start<T>(this WinApplication application, Func<IObservable<Unit>,IObservable<T>> testSelector){
            var exitSignal = new Subject<Unit>();
            return testSelector(exitSignal.AsObservable()).Merge(application.Defer(() => application.Observe()
                    .Do(winApplication => winApplication.Start())
                    .Do(_ => exitSignal.OnNext())
                    .Select(winApplication => winApplication)
                    .Catch<XafApplication, Exception>(exception => {
                        Tracing.Tracer.LogError(exception);
                        return Observable.Empty<XafApplication>();
                    }).IgnoreElements()
                    .To<T>()))
                .TakeUntil(exitSignal);
        }

    }
}