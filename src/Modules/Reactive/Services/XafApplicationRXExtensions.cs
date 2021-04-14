using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using Fasterflect;
using JetBrains.Annotations;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.XAF.ApplicationModulesManagerExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.SecurityExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using AssemblyExtensions = Xpand.Extensions.AssemblyExtensions.AssemblyExtensions;
using ListView = DevExpress.ExpressApp.ListView;
using SecurityExtensions = Xpand.XAF.Modules.Reactive.Services.Security.SecurityExtensions;
using View = DevExpress.ExpressApp.View;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class XafApplicationRxExtensions{
	    public static IObservable<T> SelectMany<T>(this XafApplication application, IObservable<T> execute) 
            => application.SelectMany(execute.ToTask);

        public static IObservable<T> SelectMany<T>(this XafApplication application, Func<IObservable<T>> execute) 
            => Observable.Defer(() => Observable.Start(execute).Merge().Wait().ReturnObservable()).Catch<T,InvalidOperationException>(_ => Observable.Empty<T>());
        
	    public static IObservable<T> SelectMany<T>(this XafApplication application, Func<Task<T>> execute) 
            => application.GetPlatform()==Platform.Web?Task.Run(execute).Result.ReturnObservable():Observable.FromAsync(execute);
        
        public static IObservable<Unit> Logon(this XafApplication application,object userKey) 
            => SecurityExtensions.AuthenticateSubject.Where(_ => _.authentication== application.Security.GetPropertyValue("Authentication"))
                .Do(_ => _.args.Instance=userKey).SelectMany(_ => application.WhenLoggedOn().FirstAsync()).ToUnit()
                .Merge(Unit.Default.ReturnObservable().Do(_ => application.Logon()).IgnoreElements());

        public static IObservable<TSource> BufferUntilCompatibilityChecked<TSource>(this XafApplication application,IObservable<TSource> source) 
            => source.Buffer(application.WhenCompatibilityChecked().FirstAsync()).FirstAsync().SelectMany(list => list)
                .Concat(Observable.Defer(() => source)).Select(source1 => source1);

        public static IObservable<XafApplication> WhenCompatibilityChecked(this XafApplication application) 
            => (bool) application.GetPropertyValue("IsCompatibilityChecked")
                ? application.ReturnObservable() : application.WhenObjectSpaceCreated().FirstAsync()
                    .Select(_ => application).TraceRX();

        [PublicAPI]
        public static IObservable<XafApplication> WhenModule(this IObservable<XafApplication> source, Type moduleType) 
            => source.Where(_ => _.Modules.FindModule(moduleType)!=null);

        public static IObservable<Frame> WhenFrameCreated(this XafApplication application) 
            => RxApp.Frames.Where(_ => _.Application==application)
                .TraceRX(frame => $"{frame.GetType().Name}-{frame.Context}");

        public static IObservable<NestedFrame> WhenNestedFrameCreated(this XafApplication application) 
            => application.WhenFrameCreated().OfType<NestedFrame>();

        public static IObservable<T> ToController<T>(this IObservable<Frame> source) where T : Controller 
            => source.SelectMany(window => window.Controllers.Cast<Controller>())
                .Select(controller => controller).OfType<T>()
                .Select(controller => controller);

        public static IObservable<Controller> ToController(this IObservable<Window> source,params string[] names) 
            => source.SelectMany(_ => _.Controllers.Cast<Controller>().Where(controller =>
                names.Contains(controller.Name))).Select(controller => controller);

        [PublicAPI]
        public static IObservable<ActionBaseEventArgs> WhenActionExecuted<TController,TAction>(
            this XafApplication application, Func<TController, TAction> action) where TController : Controller where TAction:ActionBase
            => application.WhenWindowCreated().ToController<TController>().SelectMany(_ => action(_).WhenExecuted());

        [PublicAPI]
        public static IObservable<(TAction action, CancelEventArgs e)> WhenActionExecuting<TController,TAction>(
            this XafApplication application, Func<TController, TAction> action) where TController : Controller where TAction:ActionBase 
            => application.WhenWindowCreated().ToController<TController>().SelectMany(_ => action(_).WhenExecuting());

        [PublicAPI]
        public static IObservable<ActionBaseEventArgs> WhenActionExecuteCompleted<TController,TAction>(
            this XafApplication application, Func<TController, TAction> action) where TController : Controller where TAction:ActionBase
            => application.WhenWindowCreated().ToController<TController>().SelectMany(_ => action(_).WhenExecuteCompleted());

        public static IObservable<Window> WhenWindowCreated(this XafApplication application,bool isMain=false){
            var windowCreated = application.WhenFrameCreated().OfType<Window>();
            return isMain ? WhenMainWindowAvailable(application, windowCreated) : windowCreated.TraceRX(window => window.Context);
        }

        private static IObservable<Window> WhenMainWindowAvailable(XafApplication application, IObservable<Window> windowCreated) 
            => windowCreated.When(TemplateContext.ApplicationWindow)
                .TemplateChanged()
                .SelectMany(_ => Observable.Interval(TimeSpan.FromMilliseconds(300))
                    .ObserveOnWindows(SynchronizationContext.Current)
                    .Select(_ => application.MainWindow))
                .WhenNotDefault()
                .Select(window => window).Publish().RefCount().FirstAsync()
                .TraceRX(window => window.Context);

        public static IObservable<Window> WhenPopupWindowCreated(this XafApplication application) 
            => RxApp.PopupWindows.Where(_ => _.Application==application);

        public static void AddObjectSpaceProvider(this XafApplication application, params IObjectSpaceProvider[] objectSpaceProviders) 
            => application.WhenCreateCustomObjectSpaceProvider()
                .SelectMany(t => application.WhenWeb()
                    .Do(api => application.AddObjectSpaceProvider(objectSpaceProviders, t, api.GetService<NonPersistentObjectSpaceProvider>())).ToUnit()
                    .SwitchIfEmpty(Unit.Default.ReturnObservable().Do(_ => application.AddObjectSpaceProvider(objectSpaceProviders, t))))
                .Subscribe();

        private static void AddObjectSpaceProvider(this XafApplication application,
            IObjectSpaceProvider[] objectSpaceProviders,
            (XafApplication application, CreateCustomObjectSpaceProviderEventArgs e) t,
            NonPersistentObjectSpaceProvider nonPersistentObjectSpaceProvider = null) {
            nonPersistentObjectSpaceProvider??=new NonPersistentObjectSpaceProvider(t.application.TypesInfo,null);
            if (!objectSpaceProviders.Any()) {
                t.e.ObjectSpaceProviders.Add(application.NewObjectSpaceProvider());

                t.e.ObjectSpaceProviders.Add(nonPersistentObjectSpaceProvider);
            }
            else {
                t.e.ObjectSpaceProviders.AddRange(objectSpaceProviders);
            }
        }

        public static IObjectSpaceProvider NewObjectSpaceProvider(this XafApplication application, object dataStoreProvider=null) {
            var xpoAssembly = AppDomain.CurrentDomain.GetAssemblies().First(asm => asm.GetName().Name.StartsWith("DevExpress.ExpressApp.Xpo.v"));
            dataStoreProvider??= $"{application.ConnectionString}".Contains("XpoProvider=InMemoryDataStoreProvider") || $"{application.ConnectionString}" == ""
                ? xpoAssembly.GetType("DevExpress.ExpressApp.Xpo.MemoryDataStoreProvider").CreateInstance()
                : Activator.CreateInstance(xpoAssembly.GetType("DevExpress.ExpressApp.Xpo.ConnectionStringDataStoreProvider"), application.ConnectionString);
            
            Type[] parameterTypes = {xpoAssembly.GetType("DevExpress.ExpressApp.Xpo.IXpoDataStoreProvider"), typeof(bool)};
            object[] parameterValues = {dataStoreProvider, true};
            if (application.TypesInfo.XAFVersion() > Version.Parse("19.2.0.0")) {
                parameterTypes = parameterTypes.Concat(typeof(bool).YieldItem()).ToArray();
                parameterValues = parameterValues.Concat(true.YieldItem().Cast<object>()).ToArray();
            }

            return (IObjectSpaceProvider) xpoAssembly
                .GetType("DevExpress.ExpressApp.Xpo.XPObjectSpaceProvider")
                .Constructor(parameterTypes)
                .Invoke(parameterValues);
        }

        public static IObservable<IModelApplication> WhenModelChanged(this XafApplication application) 
            => Observable.FromEventPattern<EventHandler,EventArgs>(h => application.ModelChanged += h,h => application.ModelChanged -= h,ImmediateScheduler.Instance)
                .Select(pattern => (XafApplication)pattern.Sender).Select(xafApplication =>xafApplication.Model )
                .TraceRX();

        public static IObservable<(XafApplication application, CreateCustomObjectSpaceProviderEventArgs e)> WhenCreateCustomObjectSpaceProvider(this XafApplication application) 
            => Observable.FromEventPattern<EventHandler<CreateCustomObjectSpaceProviderEventArgs>,CreateCustomObjectSpaceProviderEventArgs>(h => application.CreateCustomObjectSpaceProvider += h,h => application.CreateCustomObjectSpaceProvider -= h,ImmediateScheduler.Instance)
                .TransformPattern<CreateCustomObjectSpaceProviderEventArgs,XafApplication>()
                .TraceRX(_ => _.e.ConnectionString);

        public static IObservable<(XafApplication application, CreateCustomTemplateEventArgs e)> WhenCreateCustomTemplate(this XafApplication application) 
            => Observable.FromEventPattern<EventHandler<CreateCustomTemplateEventArgs>,CreateCustomTemplateEventArgs>(h => application.CreateCustomTemplate += h,h => application.CreateCustomTemplate -= h,ImmediateScheduler.Instance)
                .TransformPattern<CreateCustomTemplateEventArgs,XafApplication>()
                .TraceRX(_ => _.e.Context);

        [PublicAPI]
        public static IObservable<IObjectSpace> WhenObjectSpaceCreated(this IObservable<XafApplication> source) 
            => source.SelectMany(application => application.WhenObjectSpaceCreated());

        public static IObservable<(XafApplication application, NonPersistentObjectSpace ObjectSpace)> WhenNonPersistentObjectSpaceCreated(this XafApplication application)
            => application.WhenObjectSpaceCreated(true).Where(objectSpace => objectSpace is NonPersistentObjectSpace).Select(objectSpace => (application,(NonPersistentObjectSpace)objectSpace));

        public static IObservable<IObjectSpace> WhenObjectSpaceCreated(this XafApplication application,bool includeNonPersistent=false) 
            => Observable.FromEventPattern<EventHandler<ObjectSpaceCreatedEventArgs>,ObjectSpaceCreatedEventArgs>(h => application.ObjectSpaceCreated += h,h => application.ObjectSpaceCreated -= h,ImmediateScheduler.Instance)
                .TransformPattern<ObjectSpaceCreatedEventArgs,XafApplication>()
                .Where(_ => includeNonPersistent || !(_.e.ObjectSpace is NonPersistentObjectSpace))
                .TraceRX(_ => _.e.ObjectSpace.ToString())
                .Select(t => t.e.ObjectSpace);

        [PublicAPI]
        public static IObservable<XafApplication> SetupComplete(this IObservable<XafApplication> source) 
            => source.SelectMany(application => application.WhenSetupComplete());

        [PublicAPI]
        public static IObservable<ListView> ToListView(this IObservable<(XafApplication application, ListViewCreatedEventArgs e)> source) 
            => source.Select(_ => _.e.ListView);

        [PublicAPI]
        public static IObservable<TView> ToObjectView<TView>(this IObservable<(ObjectView view, EventArgs e)> source) where TView:View 
            => source.Where(_ => _.view is TView).Select(_ => _.view).Cast<TView>();

        public static IObservable<DetailView> ToDetailView(this IObservable<(XafApplication application, DetailViewCreatedEventArgs e)> source) 
            => source.Select(_ => _.e.View);

        [PublicAPI]
        public static IObservable<Frame> WhenViewOnFrame(this IObservable<XafApplication> source, Type objectType = null,
            ViewType viewType = ViewType.Any, Nesting nesting = Nesting.Any) 
            => source.SelectMany(application => application.WhenViewOnFrame(objectType, viewType, nesting));

        public static IObservable<Frame> WhenViewOnFrame(this XafApplication application,Type objectType=null,ViewType viewType=ViewType.Any,Nesting nesting=Nesting.Any) 
            => application.WhenFrameCreated().TemplateViewChanged()
	            .Where(frame => nesting==Nesting.Any|| frame is NestedFrame&&nesting==Nesting.Nested||!(frame is NestedFrame)&&nesting==Nesting.Root)
                .SelectMany(window => (window.View.ReturnObservable().When(objectType, viewType, nesting)).To(window))
                .TraceRX(window => window.View.Id);

        public static IEnumerable<Frame> WhenFrame<T>(this IEnumerable<T> source, Type objectType = null,
            ViewType viewType = ViewType.Any, Nesting nesting = Nesting.Any) where T:Frame
            => source.ToObservable(Scheduler.Immediate).WhenFrame(objectType,viewType,nesting).ToEnumerable();

        public static IObservable<T> WhenFrame<T>(this IObservable<T> source, params Type[] objectTypes) where T:Frame 
            => source.Where(frame => frame.Is(objectTypes));
        
        public static IObservable<T> WhenFrame<T>(this IObservable<T> source, params Nesting[] nesting) where T:Frame 
            => source.Where(frame => frame.Is(nesting));

        public static IObservable<T> WhenFrame<T>(this IObservable<T> source, params ViewType[] viewTypes) where T : Frame
            => source.Where(frame => frame.Is(viewTypes));

        public static IObservable<T> WhenFrame<T>(this IObservable<T> source, Type objectType = null,
            ViewType viewType = ViewType.Any, Nesting nesting = Nesting.Any) where T:Frame
            => source.Where(frame => frame.Is(nesting))
                .SelectMany(frame => frame.View != null ? frame.Is(viewType) && frame.Is(objectType) ? frame.ReturnObservable() : Observable.Empty<T>()
                    : frame.WhenViewChanged().Where(t => t.frame.Is(viewType) && t.frame.Is(objectType)).To(frame));


        public static IObservable<Frame> WhenFrameViewChanged(this XafApplication application) 
            => application.WhenFrameCreated().Merge(application.MainWindow.ReturnObservable().WhenNotDefault())
                .WhenViewChanged().Select(tuple => tuple.frame)
                .StartWith(application.MainWindow).WhenNotDefault();
        
        public static IObservable<Frame> WhenFrameViewControls(this XafApplication application) 
            => application.WhenFrameViewChanged().SelectMany(frame => frame.View.WhenControlsCreated().Select(view => view).To(frame));

        [PublicAPI]
        public static IObservable<(XafApplication application, DetailViewCreatingEventArgs e)> WhenDetailViewCreating(this XafApplication application) 
            => Observable.FromEventPattern<EventHandler<DetailViewCreatingEventArgs>, DetailViewCreatingEventArgs>(
                    h => application.DetailViewCreating += h, h => application.DetailViewCreating -= h, ImmediateScheduler.Instance)
                .TransformPattern<DetailViewCreatingEventArgs,XafApplication>()
                .TraceRX(_ => _.e.ViewID);

        public static IObservable<(XafApplication application, DetailViewCreatedEventArgs e)> WhenDetailViewCreated(this XafApplication application,Type objectType) 
            => application.WhenDetailViewCreated().Where(_ => objectType.IsAssignableFrom(_.e.View.ObjectTypeInfo.Type));

        [PublicAPI]
        public static IObservable<(XafApplication application, ListViewCreatingEventArgs args)> WhenListViewCreating(this IObservable<XafApplication> source,Type objectType=null,bool? isRoot=null) 
            => source.SelectMany(application => application.WhenListViewCreating(objectType,isRoot));

        public static IObservable<(XafApplication application, ListViewCreatingEventArgs e)> WhenListViewCreating(this XafApplication application,Type objectType=null,bool? isRoot=null) 
            => Observable.FromEventPattern<EventHandler<ListViewCreatingEventArgs>, ListViewCreatingEventArgs>(
                    h => application.ListViewCreating += h, h => application.ListViewCreating -= h, ImmediateScheduler.Instance)
                .Where(pattern => (!isRoot.HasValue || pattern.EventArgs.IsRoot == isRoot) &&
                                  (objectType == null || objectType.IsAssignableFrom(pattern.EventArgs.CollectionSource.ObjectTypeInfo.Type)))
                .TransformPattern<ListViewCreatingEventArgs,XafApplication>()
                .TraceRX(_ => _.e.ViewID);

        public static IObservable<(XafApplication application, DetailViewCreatedEventArgs e)> WhenDetailViewCreated(this XafApplication application) 
            => Observable.FromEventPattern<EventHandler<DetailViewCreatedEventArgs>, DetailViewCreatedEventArgs>(
                    h => application.DetailViewCreated += h, h => application.DetailViewCreated -= h,ImmediateScheduler.Instance)
                .TransformPattern<DetailViewCreatedEventArgs,XafApplication>()
                .TraceRX(_ => _.e.View.Id);

        public static IObservable<DashboardView> WhenDashboardViewCreated(this XafApplication application) 
            => Observable.FromEventPattern<EventHandler<DashboardViewCreatedEventArgs>, DashboardViewCreatedEventArgs>(
                    h => application.DashboardViewCreated += h, h => application.DashboardViewCreated -= h,ImmediateScheduler.Instance)
                .Select(pattern => pattern.EventArgs.View)
                .TraceRX(view => view.Id);

        [PublicAPI]
        public static IObservable<(ListView listView, XafApplication application)> WhenListViewCreated(this IObservable<XafApplication> source,Type objectType=null) 
            => source.SelectMany(application => application.WhenListViewCreated(objectType).Pair(application));

        public static IObservable<ListView> WhenListViewCreated(this XafApplication application,Type objectType=null) 
            => Observable.FromEventPattern<EventHandler<ListViewCreatedEventArgs>, ListViewCreatedEventArgs>(
                    h => application.ListViewCreated += h, h => application.ListViewCreated -= h,ImmediateScheduler.Instance)
                .Select(pattern => pattern.EventArgs.ListView)
                .Where(view => objectType==null||objectType.IsAssignableFrom(view.ObjectTypeInfo.Type))
                .TraceRX(view => view.Id);

        [PublicAPI]
        public static IObservable<DashboardView> DashboardViewCreated(this IObservable<XafApplication> source) 
            => source.SelectMany(application => application.WhenDashboardViewCreated());

        [PublicAPI]
        public static IObservable<(XafApplication application, DetailViewCreatedEventArgs e)> WhenDetailViewCreated(this IObservable<XafApplication> source) 
            => source.SelectMany(application => application.WhenDetailViewCreated());

        public static IObservable<ObjectView> WhenObjectViewCreated(this XafApplication application) 
            => application.ReturnObservable().ObjectViewCreated();

        public static IObservable<ObjectView> ObjectViewCreated(this IObservable<XafApplication> source) 
            => source.ViewCreated().OfType<ObjectView>();
        
        public static IObservable<(XafApplication application, ViewCreatingEventArgs e)> WhenObjectViewCreating(this XafApplication application) 
            => application.ReturnObservable().ObjectViewCreating();

        public static IObservable<(XafApplication application, ViewCreatingEventArgs e)> ObjectViewCreating(this IObservable<XafApplication> source) 
            => source.ViewCreating().WhenNotDefault(t => t.application.Model.Views[t.e.ViewID].AsObjectView);

        public static IObservable<View> ViewCreated(this IObservable<XafApplication> source) 
            => source.SelectMany(application => application.WhenViewCreated());

        public static IObservable<View> WhenViewCreated(this XafApplication application) 
            => Observable.FromEventPattern<EventHandler<ViewCreatedEventArgs>, ViewCreatedEventArgs>(
                    h => application.ViewCreated += h, h => application.ViewCreated -= h, ImmediateScheduler.Instance)
                .Select(pattern => pattern.EventArgs.View);
        
        public static IObservable<(XafApplication application, ViewCreatingEventArgs e)> ViewCreating(this IObservable<XafApplication> source) 
            => source.SelectMany(application => application.WhenViewCreating());

        public static IObservable<(XafApplication application, ViewCreatingEventArgs e)> WhenViewCreating(this XafApplication application) 
            => Observable.FromEventPattern<EventHandler<ViewCreatingEventArgs>, ViewCreatingEventArgs>(
                    h => application.ViewCreating += h, h => application.ViewCreating -= h, ImmediateScheduler.Instance)
                .TransformPattern<ViewCreatingEventArgs,XafApplication>();

        [PublicAPI]
        public static IObservable<(Frame SourceFrame, Frame TargetFrame)> WhenViewShown(this XafApplication application) 
            => Observable
                .FromEventPattern<EventHandler<ViewShownEventArgs>,ViewShownEventArgs>(h => application.ViewShown += h,h => application.ViewShown -= h,ImmediateScheduler.Instance)
                .Select(pattern => (pattern.EventArgs.SourceFrame,pattern.EventArgs.TargetFrame))
                .TraceRX(_ => $"source:{_.SourceFrame.View.Id}, target:{_.TargetFrame.View.Id}");

        public static IObservable<(XafApplication application, DatabaseVersionMismatchEventArgs e)> AlwaysUpdateOnDatabaseVersionMismatch(this XafApplication application) 
            => application.WhenDatabaseVersionMismatch().Select(tuple => {
                var updater = tuple.e.Updater;
                var isMiddleTier = ((IObjectSpaceProvider) updater.GetFieldValue("objectSpaceProvider")).IsInstanceOf("DevExpress.ExpressApp.Security.ClientServer.MiddleTierServerObjectSpaceProvider");
                if (!isMiddleTier) {
                    updater.Update();    
                }
                tuple.e.Handled = true;
                return tuple;
            });

        public static IObservable<(XafApplication application, DatabaseVersionMismatchEventArgs e)> WhenDatabaseVersionMismatch(this XafApplication application) 
            => Observable
                .FromEventPattern<EventHandler<DatabaseVersionMismatchEventArgs>,DatabaseVersionMismatchEventArgs>(h => application.DatabaseVersionMismatch += h,h => application.DatabaseVersionMismatch -= h,ImmediateScheduler.Instance)
                .TransformPattern<DatabaseVersionMismatchEventArgs,XafApplication>();

        [PublicAPI]
        public static IObservable<(XafApplication application, LogonEventArgs e)> WhenLoggedOn(this IObservable<XafApplication> source) 
            => source.SelectMany(application => application.WhenLoggedOn());

        public static IObservable<(XafApplication application, LogonEventArgs e)> WhenLoggedOn(this XafApplication application) 
            => Observable
                .FromEventPattern<EventHandler<LogonEventArgs>,LogonEventArgs>(h => application.LoggedOn += h,h => application.LoggedOn -= h,ImmediateScheduler.Instance)
                .TransformPattern<LogonEventArgs,XafApplication>()
                .TraceRX(_ => $"{_.e.LogonParameters}");

        [PublicAPI]
        public static IObservable<(XafApplication application, LogonEventArgs e)> WhenLoggingOn(this IObservable<XafApplication> source) 
            => source.SelectMany(application => application.WhenLoggingOn());

        public static IObservable<(XafApplication application, LogonEventArgs e)> WhenLoggingOn(this XafApplication application) 
            => Observable.FromEventPattern<EventHandler<LogonEventArgs>,LogonEventArgs>(h => application.LoggingOn += h,h => application.LoggingOn -= h,ImmediateScheduler.Instance)
            .TransformPattern<LogonEventArgs,XafApplication>()
            .TraceRX(_ => $"{_.e.LogonParameters}");
        
        [PublicAPI]
        public static IObservable<(XafApplication application, LoggingOffEventArgs e)> WhenLoggingOff(this IObservable<XafApplication> source) 
            => source.SelectMany(application => application.WhenLoggingOff());

        public static IObservable<(XafApplication application, LoggingOffEventArgs e)> WhenLoggingOff(this XafApplication application) 
            => Observable.FromEventPattern<EventHandler<LoggingOffEventArgs>,LoggingOffEventArgs>(h => application.LoggingOff += h,h => application.LoggingOff -= h,ImmediateScheduler.Instance)
            .TransformPattern<LoggingOffEventArgs,XafApplication>();
        
        [PublicAPI]
        public static IObservable<XafApplication> WhenLoggedOff(this IObservable<XafApplication> source) 
            => source.SelectMany(application => application.WhenLoggedOff());

        public static IObservable<XafApplication> WhenLoggedOff(this XafApplication application) 
            => Observable.FromEventPattern<EventHandler<EventArgs>,EventArgs>(h => application.LoggedOff += h,h => application.LoggedOff -= h,ImmediateScheduler.Instance)
            .TransformPattern<XafApplication>();

        public static IObservable<XafApplication> WhenSetupComplete(this XafApplication application) 
            => Observable.FromEventPattern<EventHandler<EventArgs>,EventArgs>(h => application.SetupComplete += h,h => application.SetupComplete -= h,ImmediateScheduler.Instance)
                .TransformPattern<XafApplication>()
                .TraceRX();

        [PublicAPI]
        public static IObservable<(XafApplication application, CreateCustomModelDifferenceStoreEventArgs e)> WhenCreateCustomModelDifferenceStore(this XafApplication application) 
            => Observable
                .FromEventPattern<EventHandler<CreateCustomModelDifferenceStoreEventArgs>,
                    CreateCustomModelDifferenceStoreEventArgs>(h => application.CreateCustomModelDifferenceStore += h,
                    h => application.CreateCustomModelDifferenceStore -= h, ImmediateScheduler.Instance)
                .TransformPattern<CreateCustomModelDifferenceStoreEventArgs,XafApplication>()
                .TraceRX(_ => _.e.Store.Name);

        [PublicAPI]
        public static IObservable<(XafApplication application, CreateCustomModelDifferenceStoreEventArgs e)> WhenCreateCustomUserModelDifferenceStore(this XafApplication application) 
            => Observable.FromEventPattern<EventHandler<CreateCustomModelDifferenceStoreEventArgs>,
                    CreateCustomModelDifferenceStoreEventArgs>(h => application.CreateCustomUserModelDifferenceStore += h,
                    h => application.CreateCustomUserModelDifferenceStore -= h, ImmediateScheduler.Instance)
                .TransformPattern<CreateCustomModelDifferenceStoreEventArgs,XafApplication>()
                .TraceRX(_ => _.e.Store?.Name);

        [PublicAPI]
        public static IObservable<(XafApplication application, SetupEventArgs e)> WhenSettingUp(this XafApplication application) 
            => Observable.FromEventPattern<EventHandler<SetupEventArgs>,SetupEventArgs>(h => application.SettingUp += h,h => application.SettingUp -= h,ImmediateScheduler.Instance)
                .TransformPattern<SetupEventArgs,XafApplication>()
                .TraceRX(_ => _.e.SetupParameters.ToString());

        public static IObservable<ApplicationModulesManager> WhenApplicationModulesManager(this XafApplication application) 
            => RxApp.ApplicationModulesManager.Where(manager => manager.Application() == application);

        public static IObservable<CreateCustomPropertyCollectionSourceEventArgs> WhenCreateCustomPropertyCollectionSource(this XafApplication application) 
            => Observable.FromEventPattern<EventHandler<CreateCustomPropertyCollectionSourceEventArgs>,
                CreateCustomPropertyCollectionSourceEventArgs>(h => application.CreateCustomPropertyCollectionSource += h,
                h => application.CreateCustomPropertyCollectionSource += h, Scheduler.Immediate).Select(_ => _.EventArgs);
        
        [PublicAPI]
        public static IObservable<DatabaseUpdaterEventArgs> 
            WhenDatabaseUpdaterCreating(this XafApplication application) 
            => Observable.FromEventPattern<EventHandler<DatabaseUpdaterEventArgs>,
                DatabaseUpdaterEventArgs>(h => application.DatabaseUpdaterCreating += h,
                h => application.DatabaseUpdaterCreating += h, Scheduler.Immediate).Select(_ => _.EventArgs);

        internal static IObservable<Unit> WhenNonPersistentPropertyCollectionSource(this XafApplication application) 
            => application.WhenCreateCustomPropertyCollectionSource()
                .Where(e => e.ObjectSpace is NonPersistentObjectSpace)
                .Select(e => {
                    e.PropertyCollectionSource = new NonPersistentPropertyCollectionSource(e.ObjectSpace, e.MasterObjectType, e.MasterObject, e.MemberInfo,e.DataAccessMode, e.Mode);
                    return e.PropertyCollectionSource;
                })
                .Cast<NonPersistentPropertyCollectionSource>()
                .TakeUntilDisposed(application)
                .ToUnit();

        public static Guid CurrentUserId(this XafApplication application) 
            => application.Security.IsSecurityStrategyComplex()
                ? (Guid?) application.Security.UserId ?? Guid.Empty
                : $"{application.Title}{Environment.MachineName}{Environment.UserName}".ToGuid();

        public static IObservable<Unit> CheckBlazor(this ApplicationModulesManager manager, Type hostingStartupType, Type requiredPackage) 
            => manager.CheckBlazor(hostingStartupType.FullName, requiredPackage.Namespace);

        public static IObservable<Unit> CheckBlazor(this ApplicationModulesManager manager, string hostingStartupType, string requiredPackage) 
            => manager.WhereApplication().ToObservable().Where(_ => DesignerOnlyCalculator.IsRunTime)
                .SelectMany(application => new[] {(hostingStartupType, requiredPackage), ("Xpand.Extensions.Blazor.HostingStartup", "Xpand.Extensions.Blazor")
            }.ToObservable().SelectMany(t => application.CheckBlazor(t.Item1, t.Item2)));


        public static IObservable<Unit> CheckBlazor(this XafApplication xafApplication, string hostingStartupType, string requiredPackage) {
            if (xafApplication.GetPlatform() == Platform.Blazor) {
                var startup = AssemblyExtensions.EntryAssembly.Attributes()
                    .Where(attribute => attribute.IsInstanceOf("Microsoft.AspNetCore.Hosting.HostingStartupAttribute"))
                    .Where(attribute => ((Type) attribute.GetPropertyValue("HostingStartupType")).FullName == hostingStartupType);
                if (!startup.Any()) {
                    throw new InvalidOperationException(
                        $"Install the {requiredPackage} package in the front end project and add: [assembly: HostingStartup(typeof({hostingStartupType}))]");
                }
            }

            return Observable.Empty<Unit>();
        }
    }


}