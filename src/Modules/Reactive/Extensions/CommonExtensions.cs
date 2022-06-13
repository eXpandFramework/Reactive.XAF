using System;
using System.ComponentModel;
using System.Configuration;
using System.Net;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Base;
using HarmonyLib;
using JetBrains.Annotations;
using Xpand.Extensions.ConfigurationExtensions;
using Xpand.Extensions.ExceptionExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive.Services.Controllers;

namespace Xpand.XAF.Modules.Reactive.Extensions{
    public static class CommonExtensions{
        public static IObservable<Unit> Patch(this IComponent component,Action<Harmony> patch) {
            var id = Guid.NewGuid().ToString();
            var harmony = new Harmony(id);
            patch(harmony);
            return component.WhenDisposed().Do(_ => harmony.UnpatchAll(id)).ToUnit();
        }

        [PublicAPI]
        public static IDisposable Subscribe<T>(this IObservable<T> source, ModuleBase moduleBase){
            var takeUntil = source.TakeUntil(moduleBase.WhenDisposed());
            return moduleBase.Application!=null ? takeUntil.Subscribe(moduleBase.Application) : takeUntil.Subscribe();
        }
        
        [PublicAPI]
        public static IDisposable Subscribe<T>(this IObservable<T> source, Controller controller) 
            => source.TakeUntil(controller.WhenDeactivated()).Subscribe(controller.Application);

        public static IDisposable Subscribe<T>(this IObservable<T> source,XafApplication application) 
            => source.HandleErrors(application).Subscribe();

        public static IObservable<T> Retry<T>(this IObservable<T> source, Func<XafApplication> applicationSelector) 
            => source.RetryWhen(_ => {
                var application = applicationSelector();
                return _.Do(application.HandleException)
                    .SelectMany(e => application.GetPlatform()==Platform.Win?e.ReturnObservable():Observable.Empty<Exception>());
            });

        public static IObservable<T> Retry<T>(this IObservable<T> source, XafApplication application) 
            => source.RetryWhen(_ => _.DistinctUntilChanged().Do(application.HandleException)
                // .SelectMany(e => application.GetPlatform()==Platform.Win?e.ReturnObservable():Observable.Empty<Exception>())
            );

        public static IObservable<T> HandleErrors<T>(this IObservable<T> source, XafApplication application, CancelEventArgs args=null,Func<Exception, IObservable<T>> exceptionSelector=null) 
            => // exceptionSelector ??= (exception => Observable.Empty<T>());
            source.Catch<T, Exception>(exception => {
                if (args != null) args.Cancel = true;
                application?.HandleException( exception);
                return exception.Handle(exceptionSelector);
            });


        public static IObservable<T> Handle<T>(this Exception exception, Func<Exception, IObservable<T>> exceptionSelector = null) 
            => exception is WarningException ? default(T).ReturnObservable() : exceptionSelector != null ? exceptionSelector(exception) : Observable.Throw<T>(exception);

        [PublicAPI]
        public static IObservable<T> HandleException<T>(this IObservable<T> source,Func<Exception,IObservable<T>> exceptionSelector=null) 
            => source.Catch<T, Exception>(exception => {
                if (Tracing.IsTracerInitialized) Tracing.Tracer.LogError(exception);
                var result=Observable.Empty<T>();
                if (ConfigurationManager.AppSettings["ExceptionMailer"]!=null){
                    result = Observable.Using(() => ConfigurationManager.AppSettings.NewSmtpClient(), smtpClient => {
                        var errorMail = exception.ToMailMessage(((NetworkCredential) smtpClient.Credentials).UserName);
                        return smtpClient.SendMailAsync(errorMail).ToObservable().To(default(T));
                    });
                }
                return result.SelectMany(_ => exception is WarningException ? default(T).ReturnObservable() :
                    exceptionSelector != null ? exceptionSelector(exception) : Observable.Throw<T>(exception));
            });

        public static IObservable<(BindingListBase<T> list, ListChangedEventArgs e)> WhenListChanged<T>(this BindingListBase<T> listBase) 
            => Observable.FromEventPattern<ListChangedEventHandler, ListChangedEventArgs>(
                    h => listBase.ListChanged += h, h => listBase.ListChanged -= h, ImmediateScheduler.Instance)
                .Select(_ => (list: (BindingListBase<T>) _.Sender, e: _.EventArgs));
    }
}