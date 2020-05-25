using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using Fasterflect;
using JetBrains.Annotations;
using Xpand.Extensions.Linq;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.AppDomain;
using Xpand.Extensions.XAF.ApplicationModulesManager;
using Xpand.Extensions.XAF.TypesInfo;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using ListView = DevExpress.ExpressApp.ListView;
using View = DevExpress.ExpressApp.View;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class XafApplicationRXExtensions{
        public static IObservable<TSource> BufferUntilCompatibilityChecked<TSource>(this XafApplication application,IObservable<TSource> source) =>
            source.Buffer(application.WhenCompatibilityChecked().FirstAsync()).FirstAsync().SelectMany(list => list)
                .Concat(Observable.Defer(() => source)).Select(source1 => source1);

        public static IObservable<XafApplication> WhenCompatibilityChecked(this XafApplication application) =>
            (bool) application.GetPropertyValue("IsCompatibilityChecked")
                ? application.ReturnObservable() : application.WhenObjectSpaceCreated().FirstAsync()
                    .Select(_ => _.application).TraceRX();

        [PublicAPI]
        public static IObservable<XafApplication> WhenModule(this IObservable<XafApplication> source, Type moduleType) => source
            .Where(_ => _.Modules.FindModule(moduleType)!=null);

        public static IObservable<Frame> WhenFrameCreated(this XafApplication application) => RxApp.Frames.Where(_ => _.Application==application);

        public static IObservable<NestedFrame> WhenNestedFrameCreated(this XafApplication application) => application.WhenFrameCreated().OfType<NestedFrame>();

        public static IObservable<T> ToController<T>(this IObservable<Frame> source) where T : Controller =>
            source.SelectMany(window => window.Controllers.Cast<Controller>())
                .Select(controller => controller).OfType<T>()
                .Select(controller => controller);

        public static IObservable<Controller> ToController(this IObservable<Window> source,params string[] names) =>
            source.SelectMany(_ => _.Controllers.Cast<Controller>().Where(controller =>
                names.Contains(controller.Name))).Select(controller => controller);

        [PublicAPI]
        public static IObservable<(ActionBase action, ActionBaseEventArgs e)> WhenActionExecuted<TController>(
            this XafApplication application, Func<TController, ActionBase> action) where TController : Controller =>
            application.WhenWindowCreated().ToController<TController>().SelectMany(_ => action(_).WhenExecuted());

        [PublicAPI]
        public static IObservable<(ActionBase action, CancelEventArgs e)> WhenActionExecuting<TController>(
            this XafApplication application, Func<TController, ActionBase> action) where TController : Controller =>
            application.WhenWindowCreated().ToController<TController>().SelectMany(_ => action(_).WhenExecuting());

        [PublicAPI]
        public static IObservable<(ActionBase action, ActionBaseEventArgs e)> WhenActionExecuteCompleted<TController>(
            this XafApplication application, Func<TController, ActionBase> action) where TController : Controller =>
            application.WhenWindowCreated().ToController<TController>().SelectMany(_ => action(_).WhenExecuteCompleted());

        public static IObservable<Window> WhenWindowCreated(this XafApplication application,bool isMain=false){
            var windowCreated = application.WhenFrameCreated().OfType<Window>();
            return isMain ? WhenMainWindowAvailable(application, windowCreated) : windowCreated.TraceRX(window => window.Context);
        }

        private static IObservable<Window> WhenMainWindowAvailable(XafApplication application, IObservable<Window> windowCreated) =>
            windowCreated.When(TemplateContext.ApplicationWindow)
                .TemplateChanged()
                .SelectMany(_ => Observable.Interval(TimeSpan.FromMilliseconds(300))
                    .ObserveOn((Control) _.Template)
                    .Select(l => application.MainWindow))
                .WhenNotDefault()
                .Select(window => window).Publish().RefCount().FirstAsync()
                .TraceRX(window => window.Context);

        public static IObservable<Window> WhenPopupWindowCreated(this XafApplication application) => RxApp.PopupWindows.Where(_ => _.Application==application);

        public static void AddObjectSpaceProvider(this XafApplication application, params IObjectSpaceProvider[] objectSpaceProviders) =>
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

        public static IObservable<IModelApplication> WhenModelChanged(this XafApplication application) =>
            Observable.FromEventPattern<EventHandler,EventArgs>(h => application.ModelChanged += h,h => application.ModelChanged -= h,ImmediateScheduler.Instance)
                .Select(pattern => (XafApplication)pattern.Sender).Select(xafApplication =>xafApplication.Model )
                .TraceRX();

        public static IObservable<ITypesInfo> WhenCustomizingTypesInfo(this XafApplication application) =>
            application.Modules.OfType<ReactiveModule>().ToObservable()
                .SelectMany(_ => _.SetupCompleted).Cast<ReactiveModule>().SelectMany(_ => _.ModifyTypesInfo)
                .TraceRX();

        public static IObservable<(XafApplication application, CreateCustomObjectSpaceProviderEventArgs e)> WhenCreateCustomObjectSpaceProvider(this XafApplication application) =>
            Observable
                .FromEventPattern<EventHandler<CreateCustomObjectSpaceProviderEventArgs>,CreateCustomObjectSpaceProviderEventArgs>(h => application.CreateCustomObjectSpaceProvider += h,h => application.CreateCustomObjectSpaceProvider -= h,ImmediateScheduler.Instance)
                .TransformPattern<CreateCustomObjectSpaceProviderEventArgs,XafApplication>()
                .TraceRX(_ => _.e.ConnectionString);

        public static IObservable<(XafApplication application, CreateCustomTemplateEventArgs e)> WhenCreateCustomTemplate(this XafApplication application) =>
            Observable
                .FromEventPattern<EventHandler<CreateCustomTemplateEventArgs>,CreateCustomTemplateEventArgs>(h => application.CreateCustomTemplate += h,h => application.CreateCustomTemplate -= h,ImmediateScheduler.Instance)
                .TransformPattern<CreateCustomTemplateEventArgs,XafApplication>()
                .TraceRX(_ => _.e.Context);

        [PublicAPI]
        public static IObservable<(XafApplication application, ObjectSpaceCreatedEventArgs e)> WhenObjectSpaceCreated(this IObservable<XafApplication> source) => source
            .SelectMany(application => application.WhenObjectSpaceCreated());

        public static IObservable<(XafApplication application, ObjectSpaceCreatedEventArgs e)> WhenObjectSpaceCreated(this XafApplication application,bool includeNonPersistent=false) =>
            Observable
                .FromEventPattern<EventHandler<ObjectSpaceCreatedEventArgs>,ObjectSpaceCreatedEventArgs>(h => application.ObjectSpaceCreated += h,h => application.ObjectSpaceCreated -= h,ImmediateScheduler.Instance)
                .TransformPattern<ObjectSpaceCreatedEventArgs,XafApplication>()
                .Where(_ => includeNonPersistent || !(_.e.ObjectSpace is NonPersistentObjectSpace))
                .TraceRX(_ => _.e.ObjectSpace.ToString());

        [PublicAPI]
        public static IObservable<XafApplication> SetupComplete(this IObservable<XafApplication> source) => source
            .SelectMany(application => application.WhenSetupComplete());

        public static IObservable<View> ViewCreated(this IObservable<XafApplication> source) => source.SelectMany(application => application.WhenViewCreated());

        [PublicAPI]
        public static IObservable<ListView> ToListView(this IObservable<(XafApplication application, ListViewCreatedEventArgs e)> source) => source.Select(_ => _.e.ListView);

        [PublicAPI]
        public static IObservable<TView> ToObjectView<TView>(this IObservable<(ObjectView view, EventArgs e)> source) where TView:View =>
            source.Where(_ => _.view is TView).Select(_ => _.view).Cast<TView>();

        public static IObservable<DetailView> ToDetailView(this IObservable<(XafApplication application, DetailViewCreatedEventArgs e)> source) => source.Select(_ => _.e.View);

        public static IObservable<(HandledEventArgs handledEventArgs, Exception exception, Exception originalException)> WhenCustomHandleException(this IObservable<IXAFAppWinAPI> source) =>
            source.SelectMany(api => Observable.FromEventPattern(api.Application, "CustomHandleException")
                .Select(pattern => (((HandledEventArgs) pattern.EventArgs), exception:((Exception) pattern.EventArgs.GetPropertyValue("Exception")
                    ),originalException:((Exception) pattern.EventArgs.GetPropertyValue("Exception")))));

        [PublicAPI]
        public static IObservable<Frame> WhenViewOnFrame(this IObservable<XafApplication> source, Type objectType = null,
            ViewType viewType = ViewType.Any, Nesting nesting = Nesting.Any) =>
            source.SelectMany(application => application.WhenViewOnFrame(objectType, viewType, nesting));

        public static IObservable<Frame> WhenViewOnFrame(this XafApplication application,Type objectType=null,ViewType viewType=ViewType.Any,Nesting nesting=Nesting.Any) =>
            application.WhenWindowCreated().TemplateViewChanged()
                .SelectMany(window => (window.View.ReturnObservable().When(objectType, viewType, nesting)).To(window))
                .TraceRX(window => window.View.Id);

        [PublicAPI]
        public static IObservable<(XafApplication application, DetailViewCreatingEventArgs e)> WhenDetailViewCreating(this XafApplication application) =>
            Observable.FromEventPattern<EventHandler<DetailViewCreatingEventArgs>, DetailViewCreatingEventArgs>(
                    h => application.DetailViewCreating += h, h => application.DetailViewCreating -= h, ImmediateScheduler.Instance)
                .TransformPattern<DetailViewCreatingEventArgs,XafApplication>()
                .TraceRX(_ => _.e.ViewID);

        public static IObservable<(XafApplication application, DetailViewCreatedEventArgs e)> WhenDetailViewCreated(this XafApplication application,Type objectType) => application
            .WhenDetailViewCreated().Where(_ => objectType.IsAssignableFrom(_.e.View.ObjectTypeInfo.Type));

        [PublicAPI]
        public static IObservable<(XafApplication application, ListViewCreatingEventArgs args)> WhenListViewCreating(this IObservable<XafApplication> source,Type objectType=null,bool? isRoot=null) => source
            .SelectMany(application => application.WhenListViewCreating(objectType,isRoot));

        public static IObservable<(XafApplication application, ListViewCreatingEventArgs e)> WhenListViewCreating(this XafApplication application,Type objectType=null,bool? isRoot=null) =>
            Observable.FromEventPattern<EventHandler<ListViewCreatingEventArgs>, ListViewCreatingEventArgs>(
                    h => application.ListViewCreating += h, h => application.ListViewCreating -= h, ImmediateScheduler.Instance)
                .Where(pattern => (!isRoot.HasValue || pattern.EventArgs.IsRoot == isRoot) &&
                                  (objectType == null || objectType.IsAssignableFrom(pattern.EventArgs.CollectionSource.ObjectTypeInfo.Type)))
                .TransformPattern<ListViewCreatingEventArgs,XafApplication>()
                .TraceRX(_ => _.e.ViewID);

        public static IObservable<(XafApplication application, DetailViewCreatedEventArgs e)> WhenDetailViewCreated(this XafApplication application) => Observable
                .FromEventPattern<EventHandler<DetailViewCreatedEventArgs>, DetailViewCreatedEventArgs>(
                    h => application.DetailViewCreated += h, h => application.DetailViewCreated -= h,ImmediateScheduler.Instance)
                .TransformPattern<DetailViewCreatedEventArgs,XafApplication>()
                .TraceRX(_ => _.e.View.Id);

        public static IObservable<DashboardView> WhenDashboardViewCreated(this XafApplication application) => Observable
            .FromEventPattern<EventHandler<DashboardViewCreatedEventArgs>, DashboardViewCreatedEventArgs>(
                    h => application.DashboardViewCreated += h, h => application.DashboardViewCreated -= h,ImmediateScheduler.Instance)
                .Select(pattern => pattern.EventArgs.View)
                .TraceRX(view => view.Id);

        [PublicAPI]
        public static IObservable<(ListView listView, XafApplication application)> WhenListViewCreated(this IObservable<XafApplication> source) => source
            .SelectMany(application => application.WhenListViewCreated().Pair(application));

        public static IObservable<ListView> WhenListViewCreated(this XafApplication application) => Observable
                .FromEventPattern<EventHandler<ListViewCreatedEventArgs>, ListViewCreatedEventArgs>(
                    h => application.ListViewCreated += h, h => application.ListViewCreated -= h,ImmediateScheduler.Instance)
                .Select(pattern => pattern.EventArgs.ListView)
                .TraceRX(view => view.Id);

        public static IObservable<ObjectView> WhenObjectViewCreated(this XafApplication application) => application.ReturnObservable().ObjectViewCreated();

        [PublicAPI]
        public static IObservable<DashboardView> DashboardViewCreated(this IObservable<XafApplication> source) => source
            .SelectMany(application => application.WhenDashboardViewCreated());

        [PublicAPI]
        public static IObservable<(XafApplication application, DetailViewCreatedEventArgs e)> DetailViewCreated(this IObservable<XafApplication> source) => source
            .SelectMany(application => application.WhenDetailViewCreated());

        public static IObservable<ObjectView> ObjectViewCreated(this IObservable<XafApplication> source) => source.ViewCreated().OfType<ObjectView>();

        public static IObservable<View> WhenViewCreated(this XafApplication application) =>
            Observable
                .FromEventPattern<EventHandler<ViewCreatedEventArgs>, ViewCreatedEventArgs>(
                    h => application.ViewCreated += h, h => application.ViewCreated -= h, ImmediateScheduler.Instance)
                .Select(pattern => pattern.EventArgs.View);

        [PublicAPI]
        public static IObservable<(Frame SourceFrame, Frame TargetFrame)> WhenViewShown(this XafApplication application) =>
            Observable
                .FromEventPattern<EventHandler<ViewShownEventArgs>,ViewShownEventArgs>(h => application.ViewShown += h,h => application.ViewShown -= h,ImmediateScheduler.Instance)
                .Select(pattern => (pattern.EventArgs.SourceFrame,pattern.EventArgs.TargetFrame))
                .TraceRX(_ => $"source:{_.SourceFrame.View.Id}, target:{_.TargetFrame.View.Id}");

        public static IObservable<(XafApplication application, DatabaseVersionMismatchEventArgs e)> AlwaysUpdateOnDatabaseVersionMismatch(this XafApplication application) =>
            application.WhenDatabaseVersionMismatch().Select(tuple => {
                tuple.e.Updater.Update();
                tuple.e.Handled = true;
                return tuple;
            });

        public static IObservable<(XafApplication application, DatabaseVersionMismatchEventArgs e)> WhenDatabaseVersionMismatch(this XafApplication application) =>
            Observable
                .FromEventPattern<EventHandler<DatabaseVersionMismatchEventArgs>,DatabaseVersionMismatchEventArgs>(h => application.DatabaseVersionMismatch += h,h => application.DatabaseVersionMismatch -= h,ImmediateScheduler.Instance)
                .TransformPattern<DatabaseVersionMismatchEventArgs,XafApplication>();

        [PublicAPI]
        public static IObservable<(XafApplication application, LogonEventArgs e)> WhenLoggedOn(this IObservable<XafApplication> soure) => soure
            .SelectMany(application => application.WhenLoggedOn());

        public static IObservable<(XafApplication application, LogonEventArgs e)> WhenLoggedOn(this XafApplication application) =>
            Observable
                .FromEventPattern<EventHandler<LogonEventArgs>,LogonEventArgs>(h => application.LoggedOn += h,h => application.LoggedOn -= h,ImmediateScheduler.Instance)
                .TransformPattern<LogonEventArgs,XafApplication>()
                .TraceRX(_ => $"{_.e.LogonParameters}");

        public static IObservable<XafApplication> WhenSetupComplete(this XafApplication application) =>
            Observable.FromEventPattern<EventHandler<EventArgs>,EventArgs>(h => application.SetupComplete += h,h => application.SetupComplete -= h,ImmediateScheduler.Instance)
                .TransformPattern<XafApplication>()
                .TraceRX();

        [PublicAPI]
        public static IObservable<(XafApplication application, CreateCustomModelDifferenceStoreEventArgs e)> WhenCreateCustomModelDifferenceStore(this XafApplication application) =>
            Observable
                .FromEventPattern<EventHandler<CreateCustomModelDifferenceStoreEventArgs>,
                    CreateCustomModelDifferenceStoreEventArgs>(h => application.CreateCustomModelDifferenceStore += h,
                    h => application.CreateCustomModelDifferenceStore -= h, ImmediateScheduler.Instance)
                .TransformPattern<CreateCustomModelDifferenceStoreEventArgs,XafApplication>()
                .TraceRX(_ => _.e.Store.Name);

        [PublicAPI]
        public static IObservable<(XafApplication application, CreateCustomModelDifferenceStoreEventArgs e)> WhenCreateCustomUserModelDifferenceStore(this XafApplication application) =>
            Observable.FromEventPattern<EventHandler<CreateCustomModelDifferenceStoreEventArgs>,
                    CreateCustomModelDifferenceStoreEventArgs>(h => application.CreateCustomUserModelDifferenceStore += h,
                    h => application.CreateCustomUserModelDifferenceStore -= h, ImmediateScheduler.Instance)
                .TransformPattern<CreateCustomModelDifferenceStoreEventArgs,XafApplication>()
                .TraceRX(_ => _.e.Store?.Name);

        [PublicAPI]
        public static IObservable<(XafApplication application, SetupEventArgs e)> WhenSettingUp(this XafApplication application) =>
            Observable
                .FromEventPattern<EventHandler<SetupEventArgs>,SetupEventArgs>(h => application.SettingUp += h,h => application.SettingUp -= h,ImmediateScheduler.Instance)
                .TransformPattern<SetupEventArgs,XafApplication>()
                .TraceRX(_ => _.e.SetupParameters.ToString());

        public static IObservable<(string parameter, object result)> WhenCallBack(this IObservable<IXAFAppWebAPI> source,string parameter=null) =>
            source.SelectMany(api => api.Application.WhenWindowCreated().When(TemplateContext.ApplicationWindow)
                    .TemplateChanged()
                    .SelectMany(_ => Observable.FromEventPattern<EventArgs>(AppDomain.CurrentDomain.XAF().CurrentRequestPage(),"InitComplete",ImmediateScheduler.Instance).To(_))
                    .Select(_ => _.Template.GetPropertyValue("CallbackManager").GetPropertyValue("CallbackControl"))
                    .SelectMany(_ => Observable.FromEventPattern<EventArgs>(_,"Callback",ImmediateScheduler.Instance)))
                .Select(_ => (parameter:$"{_.EventArgs.GetPropertyValue("Parameter")}",result:_.EventArgs.GetPropertyValue("Result")))
                .Where(_ => parameter==null||_.parameter.StartsWith($"{parameter}:"));

        public static IObservable<IXAFAppWebAPI> WhenWeb(this XafApplication application){
            return application.GetPlatform() == Platform.Web ? new XAFAppWebAPI(application).ReturnObservable() : Observable.Empty<IXAFAppWebAPI>();
        }
        [PublicAPI]
        public static void SetPageError(IXAFAppWebAPI api, Exception exception) => api.Application.HandleException(exception);

        [PublicAPI]
        public static void Redirect(IXAFAppWebAPI api, string url) => AppDomain
            .CurrentDomain.XAF().WebApplicationType().GetMethod("Redirect",new[]{typeof(string)})?.Invoke(null,new object[]{url});

        public static IObservable<ApplicationModulesManager> WhenApplicationModulesManager(this XafApplication application) => RxApp
            .ApplicationModulesManager.Where(manager => manager.Application() == application);

        public static IObservable<IXAFAppWinAPI> WhenWin(this XafApplication application) => application
            .GetPlatform() == Platform.Win ? new XAFAppWinAPI(application).ReturnObservable() : Observable.Empty<IXAFAppWinAPI>();

        public static IObservable<CreateCustomPropertyCollectionSourceEventArgs> WhenCreateCustomPropertyCollectionSource(this XafApplication application) =>
            Observable.FromEventPattern<EventHandler<CreateCustomPropertyCollectionSourceEventArgs>,
                CreateCustomPropertyCollectionSourceEventArgs>(h => application.CreateCustomPropertyCollectionSource += h,
                h => application.CreateCustomPropertyCollectionSource += h, Scheduler.Immediate).Select(_ => _.EventArgs);

        public static IObservable<NonPersistePropertyCollectionSource> WhenNonPersistentPropertyCollectionSource(this XafApplication application) =>
            application.WhenCreateCustomPropertyCollectionSource()
                .Where(e => e.ObjectSpace is NonPersistentObjectSpace)
                .Select(e => {
                    e.PropertyCollectionSource = new NonPersistePropertyCollectionSource(e.ObjectSpace, e.MasterObjectType, e.MasterObject, e.MemberInfo,e.DataAccessMode, e.Mode);
                    return e.PropertyCollectionSource;
                })
                .Cast<NonPersistePropertyCollectionSource>()
                .TakeUntilDisposed(application);
    }


}