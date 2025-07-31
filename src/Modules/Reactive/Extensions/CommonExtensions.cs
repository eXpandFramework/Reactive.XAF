using System;
using System.ComponentModel;
using System.Configuration;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Base;
using HarmonyLib;

using Xpand.Extensions.ConfigurationExtensions;
using Xpand.Extensions.ExceptionExtensions;
using Xpand.Extensions.Reactive.ErrorHandling;
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
        
        public static IDisposable Subscribe<T>(this IObservable<T> source, ModuleBase module) {
            var safe = source.MakeResilient().TakeUntil(module.WhenDisposed());
            return module.Application != null ? safe.Subscribe(module.Application) : safe.Subscribe();
        }        
        
        public static IDisposable Subscribe<T>(this IObservable<T> source, Controller controller) 
            => source.TakeUntil(controller.WhenDeactivated()).Subscribe(controller.Application);

        public static IDisposable Subscribe<T>(this IObservable<T> source,XafApplication application) 
            => source.HandleErrors(application).Subscribe();

        public static IObservable<T> Retry<T>(this IObservable<T> source, Func<XafApplication> applicationSelector) 
            => source.RetryWhen(obs => {
                var application = applicationSelector();
                return obs.Do(application.HandleException)
                    .SelectMany(e => application.GetPlatform()==Platform.Win?e.Observe():Observable.Empty<Exception>());
            });

        public static IObservable<T> Retry<T>(this IObservable<T> source, XafApplication application) 
            => source.RetryWhen(obs => obs.DistinctUntilChanged().Do(application.HandleException)
                // .SelectMany(e => application.GetPlatform()==Platform.Win?e.Observe():Observable.Empty<Exception>())
            );

        public static IObservable<T> HandleErrors<T>(this IObservable<T> source, XafApplication application, CancelEventArgs args=null,Func<Exception, IObservable<T>> exceptionSelector=null) 
            => // exceptionSelector ??= (exception => Observable.Empty<T>());
            source.Catch<T, Exception>(exception => {
                if (args != null) args.Cancel = true;
                application?.HandleException( exception);
                return exception.Handle(exceptionSelector);
            });


        public static IObservable<T> Handle<T>(this Exception exception, Func<Exception, IObservable<T>> exceptionSelector = null) 
            => exception is WarningException ? default(T).Observe() : exceptionSelector != null ? exceptionSelector(exception) : exception.Throw<T>();

        
        public static IObservable<T> HandleException<T>(this IObservable<T> source,Func<Exception,IObservable<T>> exceptionSelector=null) 
            => source.Catch<T, Exception>(exception => {
                if (Tracing.IsTracerInitialized) Tracing.Tracer.LogError(exception);
                var result=Observable.Empty<T>();
                if (ConfigurationManager.AppSettings["ExceptionMailer"]!=null){
                    result = Observable.Using(() => ConfigurationManager.AppSettings.NewSmtpClient(), smtpClient => {
                        var errorMail = exception.ToMailMessage((((NetworkCredential) smtpClient.Credentials)!).UserName);
                        return smtpClient.SendMailAsync(errorMail).ToObservable().To(default(T));
                    });
                }
                return result.SelectMany(_ => exception is WarningException ? default(T).Observe() :
                    exceptionSelector != null ? exceptionSelector(exception) : exception.Throw<T>());
            });

        public static IObservable<ListChangedEventArgs> WhenListChanged<T>(this BindingListBase<T> listBase) 
            => listBase.WhenEvent<ListChangedEventArgs>(nameof(BindingListBase<T>.ListChanged));
    }
}