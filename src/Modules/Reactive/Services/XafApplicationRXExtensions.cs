using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.DC;
using Fasterflect;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Reactive.Extensions;
using ListView = DevExpress.ExpressApp.ListView;
using View = DevExpress.ExpressApp.View;

namespace Xpand.XAF.Modules.Reactive.Services{
    
    public static class XafApplicationRXExtensions{
        public static IObservable<TSource> BufferUntilCompatibilityChecked<TSource>(
            this XafApplication application,IObservable<TSource> source){
            var compatibilityCheckefd = application.WhenCompatibilityChecked().Select(xafApplication => xafApplication).FirstAsync();
            return source.Buffer(compatibilityCheckefd).FirstAsync().SelectMany(list => list)
                .Concat(Observable.Defer(() => source)).Select(source1 => source1);
        }

        public static IObservable<XafApplication> WhenCompatibilityChecked(this XafApplication application){
            if ((bool) application.GetPropertyValue("IsCompatibilityChecked")){
                return application.AsObservable();
            }
            return application.WhenObjectSpaceCreated().FirstAsync()
                .Select(_ => _.application)
                .TraceRX();
        }

        public static IObservable<XafApplication> WhenModule(
            this IObservable<XafApplication> source, Type moduleType){
            return source.Where(_ => _.Modules.FindModule(moduleType)!=null);
        }

        public static IObservable<Frame> WhenFrameCreated(this XafApplication application){
            return RxApp.Frames.Where(_ => _.Application==application);
        }

        public static IObservable<NestedFrame> WhenNestedFrameCreated(this XafApplication application){
            return application.WhenFrameCreated().OfType<NestedFrame>();
        }

        public static IObservable<T> ToController<T>(this IObservable<Frame> source) where T : Controller{
            return source.SelectMany(window => window.Controllers.Cast<Controller>())
                .Select(controller => controller).OfType<T>().Select(controller => controller);
        }

        public static IObservable<Controller> ToController(this IObservable<Window> source,params string[] names){
            return source.SelectMany(_ => _.Controllers.Cast<Controller>().Where(controller =>
                names.Contains(controller.Name))).Select(controller => controller);
        }

        public static IObservable<(ActionBase action, ActionBaseEventArgs e)> WhenActionExecuted<TController>(
            this XafApplication application, Func<TController, ActionBase> action) where TController : Controller {
            return application.WhenWindowCreated().ToController<TController>().SelectMany(_ => action(_).WhenExecuted());
        }

        public static IObservable<(ActionBase action, CancelEventArgs e)> WhenActionExecuting<TController>(
            this XafApplication application, Func<TController, ActionBase> action) where TController : Controller {
            return application.WhenWindowCreated().ToController<TController>().SelectMany(_ => action(_).WhenExecuting());
        }

        public static IObservable<(ActionBase action, ActionBaseEventArgs e)> WhenActionExecuteCompleted<TController>(
            this XafApplication application, Func<TController, ActionBase> action) where TController : Controller {
            return application.WhenWindowCreated().ToController<TController>().SelectMany(_ => action(_).WhenExecuteCompleted());
        }

        public static IObservable<Window> WhenWindowCreated(this XafApplication application,bool isMain=false){
            var windowCreated = application.WhenFrameCreated().OfType<Window>();
            return isMain ? WhenMainWindowAvailable(application, windowCreated) : windowCreated.TraceRX();
        }

        private static IObservable<Window> WhenMainWindowAvailable(XafApplication application, IObservable<Window> windowCreated){
            return windowCreated.When(TemplateContext.ApplicationWindow)
                .TemplateChanged()
                .SelectMany(_ => Observable.Interval(TimeSpan.FromMilliseconds(300))
                    .ObserveOn((Control) _.Template)
                    .Select(l => application.MainWindow))
                .WhenNotDefault()
                .Select(window => window).Publish().RefCount().FirstAsync()
                .TraceRX();
        }

        public static IObservable<Window> WhenPopupWindowCreated(this XafApplication application){
            return RxApp.PopupWindows.Where(_ => _.Application==application);
        }

        public static void AddObjectSpaceProvider(this XafApplication application, IObjectSpaceProvider objectSpaceprovider) {
            application.WhenCreateCustomObjectSpaceProvider()
                .Select(_ => {
                    _.e.ObjectSpaceProviders.Add(objectSpaceprovider);
                    return _;
                })
                .Subscribe();
        }

        public static IObservable<(XafApplication sender, EventArgs e)> WhenModelChanged(this XafApplication application){
            return Observable.FromEventPattern<EventHandler,EventArgs>(h => application.ModelChanged += h,h => application.ModelChanged -= h)
                .TransformPattern<EventArgs,XafApplication>()
                .TraceRX();
        }

        public static IObservable<ITypesInfo> WhenCustomizingTypesInfo(this XafApplication application) {
            return application.Modules.OfType<ReactiveModule>().ToObservable()
                .SelectMany(_ => _.SetupCompleted).Cast<ReactiveModule>().SelectMany(_ => _.ModifyTypesInfo)
                .TraceRX();
        }

        public static IObservable<(XafApplication application, CreateCustomObjectSpaceProviderEventArgs e)> WhenCreateCustomObjectSpaceProvider(this XafApplication application){
            return Observable
                .FromEventPattern<EventHandler<CreateCustomObjectSpaceProviderEventArgs>,CreateCustomObjectSpaceProviderEventArgs>(h => application.CreateCustomObjectSpaceProvider += h,h => application.CreateCustomObjectSpaceProvider -= h)
                .TransformPattern<CreateCustomObjectSpaceProviderEventArgs,XafApplication>()
                .TraceRX();
        }

        public static IObservable<(XafApplication application, CreateCustomTemplateEventArgs e)> WhenCreateCustomTemplate(this XafApplication application){
            return Observable
                .FromEventPattern<EventHandler<CreateCustomTemplateEventArgs>,CreateCustomTemplateEventArgs>(h => application.CreateCustomTemplate += h,h => application.CreateCustomTemplate -= h)
                .TransformPattern<CreateCustomTemplateEventArgs,XafApplication>()
                .TraceRX();
        }

        public static IObservable<(XafApplication application, ObjectSpaceCreatedEventArgs e)> ObjectSpaceCreated(this IObservable<XafApplication> source){
            return source.SelectMany(application => application.WhenObjectSpaceCreated());
        }

        public static IObservable<(XafApplication application, ObjectSpaceCreatedEventArgs e)> WhenObjectSpaceCreated(this XafApplication application){
            return Observable
                .FromEventPattern<EventHandler<ObjectSpaceCreatedEventArgs>,ObjectSpaceCreatedEventArgs>(h => application.ObjectSpaceCreated += h,h => application.ObjectSpaceCreated -= h)
                .TransformPattern<ObjectSpaceCreatedEventArgs,XafApplication>()
                .TraceRX();
        }
        public static IObservable<(XafApplication application, EventArgs e)> SetupComplete(this IObservable<XafApplication> source){
            return source.SelectMany(application => application.WhenSetupComplete());
        }

        public static IObservable<View> ViewCreated(this IObservable<XafApplication> source){
            return source.SelectMany(application => application.WhenViewCreated());
        }

        public static IObservable<ListView> ToListView(this IObservable<(XafApplication application, ListViewCreatedEventArgs e)> source){
            return source.Select(_ => _.e.ListView);
        }

        public static IObservable<TView> ToObjectView<TView>(
            this IObservable<(ObjectView view, EventArgs e)> source) where TView:View{
            return source.Where(_ => _.view is TView).Select(_ => _.view).Cast<TView>();
        }

        public static IObservable<DetailView> ToDetailView(this IObservable<(XafApplication application, DetailViewCreatedEventArgs e)> source) {
            return source.Select(_ => _.e.View);
        }

        public static IObservable<Frame> WhenViewOnFrame(
            this XafApplication application,Type objectType=null,ViewType viewType=ViewType.Any,Nesting nesting=Nesting.Any){
            return application.WhenWindowCreated().TemplateViewChanged()
                .SelectMany(window => (window.View.AsObservable().When(objectType, viewType, nesting)).To(window))
                .TraceRX();
        }

        public static IObservable<(XafApplication application, DetailViewCreatedEventArgs e)> WhenDetailViewCreated(this XafApplication application){
            return Observable
                .FromEventPattern<EventHandler<DetailViewCreatedEventArgs>, DetailViewCreatedEventArgs>(
                    h => application.DetailViewCreated += h, h => application.DetailViewCreated -= h)
                .TransformPattern<DetailViewCreatedEventArgs, XafApplication>()
                .Select(tuple => tuple)
                .TraceRX();
        }

        public static IObservable<DashboardView> WhenDashboardViewCreated(this XafApplication application){
            return Observable
                .FromEventPattern<EventHandler<DashboardViewCreatedEventArgs>, DashboardViewCreatedEventArgs>(
                    h => application.DashboardViewCreated += h, h => application.DashboardViewCreated -= h)
                .Select(pattern => pattern.EventArgs.View).TraceRX();
        }

        public static IObservable<(XafApplication application, ListViewCreatedEventArgs e)> ListViewCreated(this IObservable<XafApplication> source){
            return source.SelectMany(application => application.WhenListViewCreated());
        }
        public static IObservable<(XafApplication application, ListViewCreatedEventArgs e)> WhenListViewCreated(this XafApplication application){
            return Observable
                .FromEventPattern<EventHandler<ListViewCreatedEventArgs>, ListViewCreatedEventArgs>(
                    h => application.ListViewCreated += h, h => application.ListViewCreated -= h)
                .TransformPattern<ListViewCreatedEventArgs, XafApplication>().TraceRX();
        }
        public static IObservable<ObjectView> WhenObjectViewCreated(this XafApplication application){
            return application.AsObservable().ObjectViewCreated();
        }

        public static IObservable<DashboardView> DashboardViewCreated(this IObservable<XafApplication> source){
            return source.SelectMany(application => application.WhenDashboardViewCreated());
        }

        public static IObservable<(XafApplication application, DetailViewCreatedEventArgs e)> DetailViewCreated(this IObservable<XafApplication> source){
            return source.SelectMany(application => application.WhenDetailViewCreated());
        }

        public static IObservable<ObjectView> ObjectViewCreated(this IObservable<XafApplication> source){
            return source.ViewCreated().OfType<ObjectView>();
        }

        public static IObservable<View> WhenViewCreated(this XafApplication application){
            return Observable
                .FromEventPattern<EventHandler<ViewCreatedEventArgs>,ViewCreatedEventArgs>(h => application.ViewCreated += h,h => application.ViewCreated -= h)
                .Select(pattern => pattern.EventArgs.View);
        }

        public static IObservable<(Frame SourceFrame, Frame TargetFrame)> WhenViewShown(this XafApplication application){
            return Observable
                .FromEventPattern<EventHandler<ViewShownEventArgs>,ViewShownEventArgs>(h => application.ViewShown += h,h => application.ViewShown -= h)
                .Select(pattern => (pattern.EventArgs.SourceFrame,pattern.EventArgs.TargetFrame))
                .TraceRX();
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
                .FromEventPattern<EventHandler<DatabaseVersionMismatchEventArgs>,DatabaseVersionMismatchEventArgs>(h => application.DatabaseVersionMismatch += h,h => application.DatabaseVersionMismatch -= h)
                .TransformPattern<DatabaseVersionMismatchEventArgs,XafApplication>();
        }

        public static IObservable<(XafApplication application, LogonEventArgs e)> WhenLoggedOn(this XafApplication application){
            return Observable
                .FromEventPattern<EventHandler<LogonEventArgs>,LogonEventArgs>(h => application.LoggedOn += h,h => application.LoggedOn -= h)
                .TransformPattern<LogonEventArgs,XafApplication>()
                .TraceRX();
        }

        public static IObservable<(XafApplication application, EventArgs e)> WhenSetupComplete(this XafApplication application){
            return Observable.FromEventPattern<EventHandler<EventArgs>,EventArgs>(h => application.SetupComplete += h,h => application.SetupComplete -= h)
                .TransformPattern<EventArgs,XafApplication>()
                .Select(tuple => tuple)
                .TraceRX();
        }

        public static IObservable<(XafApplication application, CreateCustomModelDifferenceStoreEventArgs e)> WhenCreateCustomModelDifferenceStore(this XafApplication application){
            return Observable
                .FromEventPattern<EventHandler<CreateCustomModelDifferenceStoreEventArgs>,CreateCustomModelDifferenceStoreEventArgs>(h => application.CreateCustomModelDifferenceStore += h,h => application.CreateCustomModelDifferenceStore -= h)
                .TransformPattern<CreateCustomModelDifferenceStoreEventArgs,XafApplication>()
                .TraceRX();
        }

        public static IObservable<(XafApplication application, SetupEventArgs e)> WhenSettingUp(this XafApplication application){
            return Observable
                .FromEventPattern<EventHandler<SetupEventArgs>,SetupEventArgs>(h => application.SettingUp += h,h => application.SettingUp -= h)
                .TransformPattern<SetupEventArgs,XafApplication>()
                .TraceRX();
        }
    }
}