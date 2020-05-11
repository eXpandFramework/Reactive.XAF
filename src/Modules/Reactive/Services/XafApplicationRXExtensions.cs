using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.DC;
using Fasterflect;
using JetBrains.Annotations;
using Xpand.Extensions.Linq;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ApplicationModulesManager;
using Xpand.Extensions.XAF.TypesInfo;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using ListView = DevExpress.ExpressApp.ListView;
using View = DevExpress.ExpressApp.View;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class XafApplicationRXExtensions{
        public static IObservable<TSource> BufferUntilCompatibilityChecked<TSource>(this XafApplication application,IObservable<TSource> source){
            var compatibilityCheckefd = application.WhenCompatibilityChecked().Select(xafApplication => xafApplication).FirstAsync();
            return source.Buffer(compatibilityCheckefd).FirstAsync().SelectMany(list => list)
                .Concat(Observable.Defer(() => source)).Select(source1 => source1);
        }

        public static IObservable<XafApplication> WhenCompatibilityChecked(this XafApplication application){
            if ((bool) application.GetPropertyValue("IsCompatibilityChecked")){
                return application.ReturnObservable();
            }
            return application.WhenObjectSpaceCreated().FirstAsync()
                .Select(_ => _.application)
                .TraceRX();
        }

        [PublicAPI]
        public static IObservable<XafApplication> WhenModule(this IObservable<XafApplication> source, Type moduleType){
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
                .Select(controller => controller).OfType<T>()
                .Select(controller => controller);
        }

        public static IObservable<Controller> ToController(this IObservable<Window> source,params string[] names){
            return source.SelectMany(_ => _.Controllers.Cast<Controller>().Where(controller =>
                names.Contains(controller.Name))).Select(controller => controller);
        }

        [PublicAPI]
        public static IObservable<(ActionBase action, ActionBaseEventArgs e)> WhenActionExecuted<TController>(
            this XafApplication application, Func<TController, ActionBase> action) where TController : Controller {
            return application.WhenWindowCreated().ToController<TController>().SelectMany(_ => action(_).WhenExecuted());
        }

        [PublicAPI]
        public static IObservable<(ActionBase action, CancelEventArgs e)> WhenActionExecuting<TController>(
            this XafApplication application, Func<TController, ActionBase> action) where TController : Controller {
            return application.WhenWindowCreated().ToController<TController>().SelectMany(_ => action(_).WhenExecuting());
        }

        [PublicAPI]
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

        public static void AddObjectSpaceProvider(this XafApplication application, params IObjectSpaceProvider[] objectSpaceProviders) {
            application.WhenCreateCustomObjectSpaceProvider()
                .Select(_ => {
                    if (!objectSpaceProviders.Any()){
                        var xpoASsembly = AppDomain.CurrentDomain.GetAssemblies().First(asm => asm.GetName().Name.StartsWith("DevExpress.ExpressApp.Xpo.v"));
                        var dataStoreProvider = $"{application.ConnectionString}".Contains("XpoProvider=InMemoryDataStoreProvider")||$"{application.ConnectionString}"==""
                            ? xpoASsembly.GetType("DevExpress.ExpressApp.Xpo.MemoryDataStoreProvider").CreateInstance()
                            : Activator.CreateInstance(xpoASsembly.GetType("DevExpress.ExpressApp.Xpo.ConnectionStringDataStoreProvider"),application.ConnectionString);

                        Type[] parameterTypes = {xpoASsembly.GetType("DevExpress.ExpressApp.Xpo.IXpoDataStoreProvider"), typeof(bool)};
                        object[] parameterValues = {dataStoreProvider, true};
                        if (application.TypesInfo.XAFVersion() > Version.Parse("19.2.0.0")){
                            parameterTypes=parameterTypes.Add(typeof(bool)).ToArray();
                            parameterValues=parameterValues.Add(false).ToArray();
                        }
                        var objectSpaceProvider = (IObjectSpaceProvider) xpoASsembly.GetType("DevExpress.ExpressApp.Xpo.XPObjectSpaceProvider")
                            .Constructor(parameterTypes)
                            .Invoke(parameterValues);
                        _.e.ObjectSpaceProviders.Add(objectSpaceProvider);
                        _.e.ObjectSpaceProviders.Add(new NonPersistentObjectSpaceProvider());
                    }
                    else{
                        _.e.ObjectSpaceProviders.AddRange(objectSpaceProviders);
                    }
                    return _;
                })
                .Subscribe();
        }

        public static IObservable<(XafApplication sender, EventArgs e)> WhenModelChanged(this XafApplication application){
            return Observable.FromEventPattern<EventHandler,EventArgs>(h => application.ModelChanged += h,h => application.ModelChanged -= h,ImmediateScheduler.Instance)
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
                .FromEventPattern<EventHandler<CreateCustomObjectSpaceProviderEventArgs>,CreateCustomObjectSpaceProviderEventArgs>(h => application.CreateCustomObjectSpaceProvider += h,h => application.CreateCustomObjectSpaceProvider -= h,ImmediateScheduler.Instance)
                .TransformPattern<CreateCustomObjectSpaceProviderEventArgs,XafApplication>()
                .TraceRX();
        }

        public static IObservable<(XafApplication application, CreateCustomTemplateEventArgs e)> WhenCreateCustomTemplate(this XafApplication application){
            return Observable
                .FromEventPattern<EventHandler<CreateCustomTemplateEventArgs>,CreateCustomTemplateEventArgs>(h => application.CreateCustomTemplate += h,h => application.CreateCustomTemplate -= h,ImmediateScheduler.Instance)
                .TransformPattern<CreateCustomTemplateEventArgs,XafApplication>()
                .TraceRX();
        }

        [PublicAPI]
        public static IObservable<(XafApplication application, ObjectSpaceCreatedEventArgs e)> ObjectSpaceCreated(this IObservable<XafApplication> source){
            return source.SelectMany(application => application.WhenObjectSpaceCreated());
        }

        public static IObservable<(XafApplication application, ObjectSpaceCreatedEventArgs e)> WhenObjectSpaceCreated(this XafApplication application,bool includeNonPersistent=false){
            return Observable
                .FromEventPattern<EventHandler<ObjectSpaceCreatedEventArgs>,ObjectSpaceCreatedEventArgs>(h => application.ObjectSpaceCreated += h,h => application.ObjectSpaceCreated -= h,ImmediateScheduler.Instance)
                .TransformPattern<ObjectSpaceCreatedEventArgs,XafApplication>()
                .Where(_ => includeNonPersistent || !(_.e.ObjectSpace is NonPersistentObjectSpace))
                .TraceRX();
        }
        [PublicAPI]
        public static IObservable<(XafApplication application, EventArgs e)> SetupComplete(this IObservable<XafApplication> source){
            return source.SelectMany(application => application.WhenSetupComplete());
        }

        public static IObservable<View> ViewCreated(this IObservable<XafApplication> source){
            return source.SelectMany(application => application.WhenViewCreated());
        }

        [PublicAPI]
        public static IObservable<ListView> ToListView(this IObservable<(XafApplication application, ListViewCreatedEventArgs e)> source){
            return source.Select(_ => _.e.ListView);
        }

        [PublicAPI]
        public static IObservable<TView> ToObjectView<TView>(
            this IObservable<(ObjectView view, EventArgs e)> source) where TView:View{
            return source.Where(_ => _.view is TView).Select(_ => _.view).Cast<TView>();
        }

        public static IObservable<DetailView> ToDetailView(this IObservable<(XafApplication application, DetailViewCreatedEventArgs e)> source) {
            return source.Select(_ => _.e.View);
        }

        public static IObservable<(HandledEventArgs handledEventArgs, Exception exception, Exception originalException)> WhenCustomHandleException(this IObservable<IWinAPI> source){
            return source.SelectMany(api => Observable.FromEventPattern(api.Application, "CustomHandleException")
                .Select(pattern => (((HandledEventArgs) pattern.EventArgs), exception:((Exception) pattern.EventArgs.GetPropertyValue("Exception")
                    ),originalException:((Exception) pattern.EventArgs.GetPropertyValue("Exception")))));

        }

        public static IObservable<Frame> WhenViewOnFrame(this XafApplication application,Type objectType=null,ViewType viewType=ViewType.Any,Nesting nesting=Nesting.Any){
            return application.WhenWindowCreated().TemplateViewChanged()
                .SelectMany(window => (window.View.ReturnObservable().When(objectType, viewType, nesting)).To(window))
                .TraceRX();
        }

        [PublicAPI]
        public static IObservable<DetailViewCreatingEventArgs> WhenDetailViewCreating(this XafApplication application){
            return Observable.FromEventPattern<EventHandler<DetailViewCreatingEventArgs>, DetailViewCreatingEventArgs>(
                    h => application.DetailViewCreating += h, h => application.DetailViewCreating -= h, ImmediateScheduler.Instance)
                .Select(tuple => tuple.EventArgs)
                .TraceRX();
        }

        public static IObservable<DetailViewCreatedEventArgs> WhenDetailViewCreated(this XafApplication application,Type objectType){
            return application.WhenDetailViewCreated().Where(_ => objectType.IsAssignableFrom(_.e.View.ObjectTypeInfo.Type)).Select(_ => _.e);
        }

        public static IObservable<(XafApplication application, DetailViewCreatedEventArgs e)> WhenDetailViewCreated(this XafApplication application){
            return Observable
                .FromEventPattern<EventHandler<DetailViewCreatedEventArgs>, DetailViewCreatedEventArgs>(
                    h => application.DetailViewCreated += h, h => application.DetailViewCreated -= h,ImmediateScheduler.Instance)
                .TransformPattern<DetailViewCreatedEventArgs, XafApplication>()
                .Select(tuple => tuple)
                .TraceRX();
        }

        public static IObservable<DashboardView> WhenDashboardViewCreated(this XafApplication application){
            return Observable.FromEventPattern<EventHandler<DashboardViewCreatedEventArgs>, DashboardViewCreatedEventArgs>(
                    h => application.DashboardViewCreated += h, h => application.DashboardViewCreated -= h,ImmediateScheduler.Instance)
                .Select(pattern => pattern.EventArgs.View)
                .TraceRX();
        }

        [PublicAPI]
        public static IObservable<(XafApplication application, ListViewCreatedEventArgs e)> ListViewCreated(this IObservable<XafApplication> source){
            return source.SelectMany(application => application.WhenListViewCreated());
        }
        public static IObservable<(XafApplication application, ListViewCreatedEventArgs e)> WhenListViewCreated(this XafApplication application){
            return Observable
                .FromEventPattern<EventHandler<ListViewCreatedEventArgs>, ListViewCreatedEventArgs>(
                    h => application.ListViewCreated += h, h => application.ListViewCreated -= h,ImmediateScheduler.Instance)
                .TransformPattern<ListViewCreatedEventArgs, XafApplication>()
                .TraceRX();
        }
        public static IObservable<ObjectView> WhenObjectViewCreated(this XafApplication application){
            return application.ReturnObservable().ObjectViewCreated();
        }

        [PublicAPI]
        public static IObservable<DashboardView> DashboardViewCreated(this IObservable<XafApplication> source){
            return source.SelectMany(application => application.WhenDashboardViewCreated());
        }

        [PublicAPI]
        public static IObservable<(XafApplication application, DetailViewCreatedEventArgs e)> DetailViewCreated(this IObservable<XafApplication> source){
            return source.SelectMany(application => application.WhenDetailViewCreated());
        }

        public static IObservable<ObjectView> ObjectViewCreated(this IObservable<XafApplication> source){
            return source.ViewCreated().OfType<ObjectView>();
        }

        public static IObservable<View> WhenViewCreated(this XafApplication application){
            return Observable
                .FromEventPattern<EventHandler<ViewCreatedEventArgs>, ViewCreatedEventArgs>(
                    h => application.ViewCreated += h, h => application.ViewCreated -= h, ImmediateScheduler.Instance)
                .Select(pattern => pattern.EventArgs.View);
        }

        public static IObservable<(Frame SourceFrame, Frame TargetFrame)> WhenViewShown(this XafApplication application){
            return Observable
                .FromEventPattern<EventHandler<ViewShownEventArgs>,ViewShownEventArgs>(h => application.ViewShown += h,h => application.ViewShown -= h,ImmediateScheduler.Instance)
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
                .FromEventPattern<EventHandler<DatabaseVersionMismatchEventArgs>,DatabaseVersionMismatchEventArgs>(h => application.DatabaseVersionMismatch += h,h => application.DatabaseVersionMismatch -= h,ImmediateScheduler.Instance)
                .TransformPattern<DatabaseVersionMismatchEventArgs,XafApplication>();
        }

        public static IObservable<(XafApplication application, LogonEventArgs e)> WhenLoggedOn(this XafApplication application){
            return Observable
                .FromEventPattern<EventHandler<LogonEventArgs>,LogonEventArgs>(h => application.LoggedOn += h,h => application.LoggedOn -= h,ImmediateScheduler.Instance)
                .TransformPattern<LogonEventArgs,XafApplication>()
                .TraceRX();
        }

        public static IObservable<(XafApplication application, EventArgs e)> WhenSetupComplete(this XafApplication application){
            return Observable.FromEventPattern<EventHandler<EventArgs>,EventArgs>(h => application.SetupComplete += h,h => application.SetupComplete -= h,ImmediateScheduler.Instance)
                .TransformPattern<EventArgs,XafApplication>()
                .Select(tuple => tuple)
                .TraceRX();
        }

        [PublicAPI]
        public static IObservable<(XafApplication application, CreateCustomModelDifferenceStoreEventArgs e)> WhenCreateCustomModelDifferenceStore(this XafApplication application){
            return Observable
                .FromEventPattern<EventHandler<CreateCustomModelDifferenceStoreEventArgs>,
                    CreateCustomModelDifferenceStoreEventArgs>(h => application.CreateCustomModelDifferenceStore += h,
                    h => application.CreateCustomModelDifferenceStore -= h, ImmediateScheduler.Instance)
                .TransformPattern<CreateCustomModelDifferenceStoreEventArgs,XafApplication>()
                .TraceRX();
        }

        [PublicAPI]
        public static IObservable<(XafApplication application, CreateCustomModelDifferenceStoreEventArgs e)> WhenCreateCustomUserModelDifferenceStore(this XafApplication application){
            return Observable
                .FromEventPattern<EventHandler<CreateCustomModelDifferenceStoreEventArgs>,
                    CreateCustomModelDifferenceStoreEventArgs>(h => application.CreateCustomUserModelDifferenceStore += h,
                    h => application.CreateCustomUserModelDifferenceStore -= h, ImmediateScheduler.Instance)
                .TransformPattern<CreateCustomModelDifferenceStoreEventArgs,XafApplication>()
                .TraceRX();
        }

        [PublicAPI]
        public static IObservable<(XafApplication application, SetupEventArgs e)> WhenSettingUp(this XafApplication application){
            return Observable
                .FromEventPattern<EventHandler<SetupEventArgs>,SetupEventArgs>(h => application.SettingUp += h,h => application.SettingUp -= h,ImmediateScheduler.Instance)
                .TransformPattern<SetupEventArgs,XafApplication>()
                .TraceRX();
        }

        public static IObservable<(string parameter, object result)> WhenCallBack(this IObservable<IWebAPI> source,string parameter=null){
            return source.SelectMany(api => api.Application.WhenWindowCreated().When(TemplateContext.ApplicationWindow)
                .TemplateChanged()
                .SelectMany(_ => Observable.FromEventPattern<EventArgs>(CommonExtensions.CurrentRequestPage,"InitComplete",ImmediateScheduler.Instance).To(_))
                .Select(_ => _.Template.GetPropertyValue("CallbackManager").GetPropertyValue("CallbackControl"))
                .SelectMany(_ => Observable.FromEventPattern<EventArgs>(_,"Callback",ImmediateScheduler.Instance)))
                .Select(_ => (parameter:$"{_.EventArgs.GetPropertyValue("Parameter")}",result:_.EventArgs.GetPropertyValue("Result")))
                .Where(_ => parameter==null||_.parameter.StartsWith($"{parameter}:"));
        }

        public static IObservable<IWebAPI> WhenWeb(this XafApplication application){
            return application.GetPlatform() == Platform.Web ? new WebApi(application).ReturnObservable() : Observable.Empty<IWebAPI>();
        }

        public static IObservable<ApplicationModulesManager> WhenApplicationModulesManager(this XafApplication application){
            return RxApp.ApplicationModulesManager.Where(manager => manager.Application() == application);
        }

        public static IObservable<IWinAPI> WhenWin(this XafApplication application){
            return application.GetPlatform() == Platform.Win ? new WinApi(application).ReturnObservable() : Observable.Empty<IWinAPI>();
        }
        
        public static IObservable<CreateCustomPropertyCollectionSourceEventArgs> WhenCreateCustomPropertyCollectionSource(this XafApplication application){
            return Observable.FromEventPattern<EventHandler<CreateCustomPropertyCollectionSourceEventArgs>,
                    CreateCustomPropertyCollectionSourceEventArgs>(h => application.CreateCustomPropertyCollectionSource += h,
                    h => application.CreateCustomPropertyCollectionSource += h, Scheduler.Immediate).Select(_ => _.EventArgs);
        }

        public static IObservable<NonPersistePropertyCollectionSource> WhenNonPersistentPropertyCollectionSource(this XafApplication application){
            return application.WhenCreateCustomPropertyCollectionSource()
                .Where(e => e.ObjectSpace is NonPersistentObjectSpace)
                .Select(e => {
                    e.PropertyCollectionSource = new NonPersistePropertyCollectionSource(e.ObjectSpace, e.MasterObjectType, e.MasterObject, e.MemberInfo,e.DataAccessMode, e.Mode);
                    return e.PropertyCollectionSource;
                })
                .Cast<NonPersistePropertyCollectionSource>()
                .TakeUntilDisposed(application);
        }

    }

    class WebApi:IWebAPI{
        public XafApplication Application{ get; }

        public WebApi(XafApplication application){
            Application = application;
        }

    }
    class WinApi:IWinAPI{
        public XafApplication Application{ get; }

        public WinApi(XafApplication application){
            Application = application;
        }

    }
    public interface IWebAPI{
        XafApplication Application{ get; }
    }
    public interface IWinAPI{
        XafApplication Application{ get; }
    }
}