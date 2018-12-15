using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.XAF.Modules.Reactive.Extensions;

namespace DevExpress.XAF.Modules.Reactive.Services{
    public static class XafApplicationExtensions{

        public static void RegisterAsRX(this XafApplication application) {
            RxApp.XafApplication=application;
        }

        public static void AddObjectSpaceProvider(this XafApplication application, IObjectSpaceProvider objectSpaceprovider) {
            application.WhenCreateCustomObjectSpaceProvider()
                .Select(_ => {
                    _.e.ObjectSpaceProviders.Add(objectSpaceprovider);
                    return _;
                })
                .Subscribe();
        }

        public static IObservable<ITypesInfo> WhenCustomizingTypesInfo(this XafApplication application) {
            return application.Modules.OfType<ReactiveModule>().ToObservable(Scheduler.Default)
                .Repeat()
                .FirstAsync()
                .Do(_ => {},() => {})
                .Select(_ => _.TypesInfo).Switch();
        }

        public static IObservable<(XafApplication application, CreateCustomObjectSpaceProviderEventArgs e)> WhenCreateCustomObjectSpaceProvider(this XafApplication application){
            return Observable
                .FromEventPattern<EventHandler<CreateCustomObjectSpaceProviderEventArgs>,
                    CreateCustomObjectSpaceProviderEventArgs>(h => application.CreateCustomObjectSpaceProvider += h,
                    h => application.CreateCustomObjectSpaceProvider -= h)
                .TakeUntil(application.WhenDisposed())
                .TransformPattern<CreateCustomObjectSpaceProviderEventArgs,XafApplication>();
        }

        public static IObservable<(XafApplication application, DatabaseVersionMismatchEventArgs e)> AlwaysUpdateOnDatabaseVersionMismatch(this XafApplication application){
            return application.WhenDatabaseVersionMismatch().Select(tuple => {
                tuple.e.Updater.Update();
                tuple.e.Handled = true;
                return tuple;
            });
        }

        public static IObservable<(XafApplication application, DatabaseVersionMismatchEventArgs e)> WhenDatabaseVersionMismatch(this XafApplication application){
            return Observable
                .FromEventPattern<EventHandler<DatabaseVersionMismatchEventArgs>,
                    DatabaseVersionMismatchEventArgs>(h => application.DatabaseVersionMismatch += h,
                    h => application.DatabaseVersionMismatch -= h)
                .TakeUntil(application.WhenDisposed()).TransformPattern<DatabaseVersionMismatchEventArgs,XafApplication>();
        }

        public static IObservable<(XafApplication application, LogonEventArgs e)> WhenLoggedOn(this XafApplication application){
            return Observable
                .FromEventPattern<EventHandler<LogonEventArgs>,
                    LogonEventArgs>(h => application.LoggedOn += h,
                    h => application.LoggedOn -= h)
                .TakeUntil(application.WhenDisposed())
                .TransformPattern<LogonEventArgs,XafApplication>();
        }

        public static IObservable<(XafApplication application, EventArgs e)> WhenSetupComplete(this XafApplication application){
            return Observable
                .FromEventPattern<EventHandler<EventArgs>,
                    EventArgs>(h => application.SetupComplete += h,
                    h => application.SetupComplete -= h)
                .TakeUntil(application.WhenDisposed())
                .TransformPattern<EventArgs,XafApplication>();
        }

        public static IObservable<(XafApplication application, CreateCustomModelDifferenceStoreEventArgs e)> WhenCreateCustomModelDifferenceStore(this XafApplication application){
            return Observable
                .FromEventPattern<EventHandler<CreateCustomModelDifferenceStoreEventArgs>,
                    CreateCustomModelDifferenceStoreEventArgs>(h => application.CreateCustomModelDifferenceStore += h,
                    h => application.CreateCustomModelDifferenceStore -= h)
                .TakeUntil(application.WhenDisposed())
                .TransformPattern<CreateCustomModelDifferenceStoreEventArgs,XafApplication>();
        }

        public static IObservable<(XafApplication application, SetupEventArgs e)> WhenSettingUp(this XafApplication application){
            return Observable
                .FromEventPattern<EventHandler<SetupEventArgs>,
                    SetupEventArgs>(h => application.SettingUp += h,
                    h => application.SettingUp -= h)
                .TakeUntil(application.WhenDisposed())
                .TransformPattern<SetupEventArgs,XafApplication>();
        }


    }
}