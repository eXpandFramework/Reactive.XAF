using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.SystemModule;
using Fasterflect;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.TypeExtensions;
using Xpand.Extensions.XAF.ApplicationModulesManagerExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.CollectionSourceExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.Extensions.XAF.ObjectSpaceProviderExtensions;
using Xpand.Extensions.XAF.SecurityExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Reactive.Services.Controllers;
using Xpand.XAF.Modules.Reactive.Services.Security;
using AssemblyExtensions = Xpand.Extensions.AssemblyExtensions.AssemblyExtensions;
using ListView = DevExpress.ExpressApp.ListView;
using SecurityExtensions = Xpand.XAF.Modules.Reactive.Services.Security.SecurityExtensions;
using View = DevExpress.ExpressApp.View;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class XafApplicationRxExtensions {
        
        
        static readonly ISubject<Func<IObservable<object>>> CommitChangesSubject=new Subject<Func<IObservable<object>>>();
        static XafApplicationRxExtensions(){
            CommitChangesSubject.Synchronize()
                .SelectManySequential(func => func().CompleteOnError())
                .Subscribe();
        }

        public static IObservable<T> SelectMany<T>(this XafApplication application, IObservable<T> execute) 
            => application.SelectMany(execute.ToTask);

        public static IObservable<T> SelectMany<T>(this XafApplication application, Func<IObservable<T>> execute) 
            => Observable.Defer(() => application.GetPlatform()==Platform.Web?Observable.Start(execute).Merge().Wait().ReturnObservable():Observable.Start(execute).Merge())
	            .Catch<T,InvalidOperationException>(_ => Observable.Empty<T>());
        
	    public static IObservable<T> SelectMany<T>(this XafApplication application, Func<Task<T>> execute) 
            => application.GetPlatform()==Platform.Web?Task.Run(execute).Result.ReturnObservable():Observable.FromAsync(execute);
        
        public static IObservable<Unit> LogonUser(this XafApplication application,object userKey) 
            => SecurityExtensions.AuthenticateSubject.Where(_ => _.authentication== application.Security.GetPropertyValue("Authentication"))
                .Do(_ => _.args.SetInstance(_ => userKey)).SelectMany(_ => application.WhenLoggedOn().FirstAsync()).ToUnit()
                .Merge(Unit.Default.ReturnObservable().Do(_ => application.Logon()).IgnoreElements());

        public static IObservable<TSource> BufferUntilCompatibilityChecked<TSource>(this XafApplication application,IObservable<TSource> source) 
            => source.Buffer(application.WhenCompatibilityChecked().FirstAsync()).FirstAsync().SelectMany()
                .Concat(Observable.Defer(() => source));

        public static IObservable<XafApplication> WhenCompatibilityChecked(this XafApplication application) 
            => (bool) application.GetPropertyValue("IsCompatibilityChecked")
                ? application.ReturnObservable() : application.WhenObjectSpaceCreated().FirstAsync()
                    .Select(_ => application);

        
        public static IObservable<XafApplication> WhenModule(this IObservable<XafApplication> source, Type moduleType) 
            => source.Where(_ => _.Modules.FindModule(moduleType)!=null);

        public static IObservable<Frame> WhenFrameCreated(this XafApplication application,TemplateContext templateContext=default)
            => application.WhenEvent<FrameCreatedEventArgs>(nameof(XafApplication.FrameCreated)).Select(e => e.Frame)
                .Where(frame => frame.Application==application&& (templateContext==default ||frame.Context == templateContext));

        private static readonly Subject<GenericEventArgs<XafApplication>> WhenExitingSubject = new();
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static bool Exit(XafApplication __instance) {
            var args = new GenericEventArgs<XafApplication>(__instance);
            WhenExitingSubject.OnNext(args);
            return !args.Handled;
        }

        public static IObservable<GenericEventArgs<XafApplication>> WhenExiting(this XafApplication application)
            => WhenExitingSubject.FirstAsync(t => t.Instance==application);

        public static IObservable<NestedFrame> WhenNestedFrameCreated(this XafApplication application) 
            => application.WhenFrameCreated().OfType<NestedFrame>();

        public static IObservable<T> ToController<T>(this IObservable<Frame> source) where T : Controller 
            => source.SelectMany(window => window.Controllers.Cast<Controller>())
                .Select(controller => controller).OfType<T>()
                .Select(controller => controller);

        public static IObservable<Controller> ToController(this IObservable<Window> source,params string[] names) 
            => source.SelectMany(_ => _.Controllers.Cast<Controller>().Where(controller =>
                names.Contains(controller.Name))).Select(controller => controller);

        
        public static IObservable<ActionBaseEventArgs> WhenActionExecuted<TController,TAction>(
            this XafApplication application, Func<TController, TAction> action) where TController : Controller where TAction:ActionBase
            => application.WhenFrameCreated().ToController<TController>().SelectMany(_ => action(_).WhenExecuted());
        
        public static IObservable<SimpleActionExecuteEventArgs> WhenSimpleActionExecuted<TController>(
            this XafApplication application, Func<TController, SimpleAction> action) where TController : Controller 
            => application.WhenFrameCreated().ToController<TController>().SelectMany(_ => action(_).WhenExecuted());

        public static IObservable<ActionBaseEventArgs> WhenActionExecuted(this XafApplication application,params string[] actions) 
            => application.WhenFrameCreated().SelectMany(window => window.Actions(actions)).WhenExecuted();
        public static IObservable<ActionBaseEventArgs> WhenActionExecuteCompleted(this XafApplication application,params string[] actions) 
            => application.WhenFrameCreated().SelectMany(window => window.Actions(actions)).WhenExecuteCompleted();
        public static IObservable<ActionBase> WhenActionExecuteConcat(this XafApplication application,params string[] actions) 
            => application.WhenFrameCreated().SelectMany(window => window.Actions(actions)).WhenExecuteConcat();
        
        public static IObservable<(TAction action, CancelEventArgs e)> WhenActionExecuting<TController,TAction>(
            this XafApplication application, Func<TController, TAction> action) where TController : Controller where TAction:ActionBase 
            => application.WhenFrameCreated().ToController<TController>().SelectMany(_ => action(_).WhenExecuting());
        
        public static IObservable<ActionBaseEventArgs> WhenActionExecuteCompleted<TController,TAction>(
            this XafApplication application, Func<TController, TAction> action) where TController : Controller where TAction:ActionBase
            => application.WhenFrameCreated().ToController<TController>().SelectMany(_ => action(_).WhenExecuteCompleted());

        public static IObservable<Window> WhenWindowCreated(this XafApplication application,bool isMain=false,bool emitIfMainExists=true) {
            var windowCreated = application.WhenFrameCreated().Select(frame => frame).OfType<Window>();
            return isMain ? emitIfMainExists && application.MainWindow != null ? application.MainWindow.ReturnObservable().ObserveOn(SynchronizationContext.Current!)
                    : application.WhenMainWindowAvailable(windowCreated) : windowCreated.TraceRX(window => window.Context);
        }

        private static IObservable<Window> WhenMainWindowAvailable(this XafApplication application, IObservable<Window> windowCreated) 
            => windowCreated.When(TemplateContext.ApplicationWindow).TemplateChanged()
                .SelectMany(_ => Observable.Interval(TimeSpan.FromMilliseconds(300))
                    .ObserveOnWindows(SynchronizationContext.Current)
                    .Select(_ => application.MainWindow))
                .WhenNotDefault()
                .Select(window => window).Publish().RefCount().FirstAsync()
                .TraceRX(window => window.Context);
        
        public static IObservable<Window> WhenPopupWindowCreated(this XafApplication application) 
            => application.WhenFrameCreated(TemplateContext.PopupWindow).Where(_ => _.Application==application).Cast<Window>();
        
        public static void AddObjectSpaceProvider(this XafApplication application, params IObjectSpaceProvider[] objectSpaceProviders) 
            => application.WhenCreateCustomObjectSpaceProvider()
                .SelectMany(t => application.WhenWeb()
                    .Do(api => application.AddObjectSpaceProvider(objectSpaceProviders, t, api.GetService<NonPersistentObjectSpaceProvider>())).ToUnit()
                    .SwitchIfEmpty(Unit.Default.ReturnObservable().Do(_ => application.AddObjectSpaceProvider(objectSpaceProviders, t))))
                .Subscribe();

        private static void AddObjectSpaceProvider(this XafApplication application, IObjectSpaceProvider[] objectSpaceProviders,
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

		[SuppressMessage("Design", "XAF0013:Avoid reading the XafApplication.ConnectionString property", Justification = "<Pending>")]
		public static IObjectSpaceProvider NewObjectSpaceProvider(this XafApplication application, object dataStoreProvider=null) {
            var xpoAssembly = AppDomain.CurrentDomain.GetAssemblies().First(asm => asm.GetName().Name!.StartsWith("DevExpress.ExpressApp.Xpo.v"));
            dataStoreProvider??= $"{application.ConnectionString}".Contains("XpoProvider=InMemoryDataStoreProvider") || $"{application.ConnectionString}" == ""
                ? xpoAssembly.GetType("DevExpress.ExpressApp.Xpo.MemoryDataStoreProvider").CreateInstance()
                : Activator.CreateInstance(xpoAssembly.GetType("DevExpress.ExpressApp.Xpo.ConnectionStringDataStoreProvider")!, application.ConnectionString);
            
            Type[] parameterTypes = {xpoAssembly.GetType("DevExpress.ExpressApp.Xpo.IXpoDataStoreProvider"), typeof(bool)};
            object[] parameterValues = {dataStoreProvider, true};
            if (application.TypesInfo.XAFVersion() > Version.Parse("19.2.0.0")) {
                parameterTypes = parameterTypes.Concat(typeof(bool).YieldItem()).ToArray();
                parameterValues = parameterValues.Concat(true.YieldItem().Cast<object>()).ToArray();
            }

            var type = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name=="Xpand.Extensions.XAF.Xpo")
                           ?.GetType("Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions.FastObjectSpaceProvider") ??
                       xpoAssembly.GetType("DevExpress.ExpressApp.Xpo.XPObjectSpaceProvider");
            return (IObjectSpaceProvider) type.Constructor(parameterTypes).Invoke(parameterValues);
        }

        public static IObservable<IModelApplication> WhenModelChanged(this XafApplication application) 
            => application.WhenEvent(nameof(XafApplication.ModelChanged))
                .Select(_ =>application.Model )
                .TraceRX();

        public static IObservable<(XafApplication application, CreateCustomObjectSpaceProviderEventArgs e)> WhenCreateCustomObjectSpaceProvider(this XafApplication application) 
            => application.WhenEvent<CreateCustomObjectSpaceProviderEventArgs>(nameof(XafApplication.CreateCustomObjectSpaceProvider)).InversePair(application);

        public static IObservable<(XafApplication application, CreateCustomTemplateEventArgs e)> WhenCreateCustomTemplate(this XafApplication application) 
            => application.WhenEvent<CreateCustomTemplateEventArgs>(nameof(XafApplication.CreateCustomTemplate)).InversePair(application);

        
        public static IObservable<IObjectSpace> WhenObjectSpaceCreated(this IObservable<XafApplication> source) 
            => source.SelectMany(application => application.WhenObjectSpaceCreated());

        public static IObservable<(XafApplication application, NonPersistentObjectSpace ObjectSpace)> WhenNonPersistentObjectSpaceCreated(this XafApplication application)
            => application.WhenObjectSpaceCreated().Where(objectSpace => objectSpace is NonPersistentObjectSpace).Select(objectSpace => (application,(NonPersistentObjectSpace)objectSpace));

        public static IObservable<IObjectSpace> WhenProviderObjectSpaceCreated(this XafApplication application,Func<IObjectSpaceProvider> provider=null) {
            var objectSpaceProvider = provider?.Invoke()??application.ObjectSpaceProvider;
            return application.ObjectSpaceProviders.Where(spaceProvider => spaceProvider == objectSpaceProvider).ToNowObservable()
                .SelectMany(spaceProvider => spaceProvider.WhenObjectSpaceCreated());
        }
        public static IObservable<IObjectSpace> WhenProviderObjectSpaceCreated(this XafApplication application,bool emitUpdatingObjectSpace) 
            => application.ObjectSpaceProviders.ToNowObservable()
                .SelectMany(spaceProvider => spaceProvider.WhenObjectSpaceCreated(emitUpdatingObjectSpace));

        public static IObjectSpace CreateAuthenticatedObjectSpace(this XafApplication application, string userName)  
            => application.ServiceProvider.CreateAuthenticatedObjectSpace(application.Security.UserType,userName);
        
        public static IObservable<IObjectSpace> WhenObjectSpaceCreated(this XafApplication application,bool includeNonPersistent=true,bool includeNested=false) 
            => application.WhenEvent<ObjectSpaceCreatedEventArgs>(nameof(XafApplication.ObjectSpaceCreated)).InversePair(application)
                .Where(t => (includeNonPersistent || t.source.ObjectSpace is not NonPersistentObjectSpace)&&
                            (includeNested || t.source.ObjectSpace is not INestedObjectSpace))
                .Select(t => t.source.ObjectSpace);

        
        public static IObservable<XafApplication> SetupComplete(this IObservable<XafApplication> source) 
            => source.SelectMany(application => application.WhenSetupComplete());

        
        public static IObservable<ListView> ToListView(this IObservable<(XafApplication application, ListViewCreatedEventArgs e)> source) 
            => source.Select(_ => _.e.ListView);

        
        public static IObservable<TView> ToObjectView<TView>(this IObservable<(ObjectView view, EventArgs e)> source) where TView:View 
            => source.Where(_ => _.view is TView).Select(_ => _.view).Cast<TView>();

        public static IObservable<DetailView> ToDetailView(this IObservable<(XafApplication application, DetailViewCreatedEventArgs e)> source) 
            => source.Select(_ => _.e.View);

        
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
            => source.ToObservable(Transform.ImmediateScheduler).WhenFrame(objectType,viewType,nesting).ToEnumerable();
        
        public static IEnumerable<Frame> WhenFrame<T>(this IEnumerable<T> source, params string[] viewIds) where T:Frame 
            => source.ToObservable(Transform.ImmediateScheduler).Where(arg => viewIds.Contains(arg.View.Id)).ToEnumerable();

        public static IObservable<T> WhenFrame<T>(this IObservable<T> source, params Type[] objectTypes) where T:Frame 
            => source.Where(frame => frame.Is(objectTypes));
        public static IObservable<T> WhenFrame<T>(this IObservable<T> source, params string[] viewIds) where T:Frame 
            => source.Where(frame => frame.Is(viewIds));
        
        public static IObservable<T> WhenFrame<T>(this IObservable<T> source, params Nesting[] nesting) where T:Frame 
            => source.Where(frame => frame.Is(nesting));

        public static IObservable<View> ToView<T>(this IObservable<T> source) where T : Frame
            => source.Select(frame => frame.View);
        public static IObservable<DetailView> ToDetailView<T>(this IObservable<T> source) where T : Frame
            => source.Select(frame => frame.View.AsDetailView());
        public static IObservable<ListView> ToListView<T>(this IObservable<T> source) where T : Frame
            => source.Select(frame => frame.View.AsListView());
        
        public static IObservable<T> WhenFrame<T>(this IObservable<T> source, params ViewType[] viewTypes) where T : Frame
            => source.Where(frame => frame.Is(viewTypes));

        public static IObservable<View> WhenView<TFrame>(this IObservable<TFrame> source, Type objectType = null,
            ViewType viewType = ViewType.Any, Nesting nesting = Nesting.Any) where TFrame : Frame
            => source.WhenFrame(objectType, viewType, nesting).ToView();

        public static IObservable<View> WhenDetailView<TFrame>(this IObservable<TFrame> source, params Type[] objectTypes) where TFrame : Frame
            => source.WhenFrame(objectTypes).WhenFrame(ViewType.DetailView).ToView();
        
        public static IObservable<View> WhenDetailView<TFrame>(this IObservable<TFrame> source, Type objectType = null, Nesting nesting = Nesting.Any) where TFrame : Frame
            => source.WhenFrame(objectType,ViewType.DetailView, nesting).ToView();
        
        public static IObservable<View> WhenListView<TFrame>(this IObservable<TFrame> source, Type objectType = null, Nesting nesting = Nesting.Any) where TFrame : Frame
            => source.WhenFrame(objectType,ViewType.ListView, nesting).ToView();
        
        public static IObservable<T> WhenDetailView<T,TObject>(this IObservable<T> source, Func<TObject,bool> criteria) where T:Frame
            => source.WhenFrame(typeof(TObject),ViewType.DetailView).Where(frame => criteria(frame.View.CurrentObject.As<TObject>()));
        
        public static IObservable<T> WhenFrame<T>(this IObservable<T> source, Type objectType = null,
            ViewType viewType = ViewType.Any, Nesting nesting = Nesting.Any) where T:Frame
            => source.Where(frame => frame.Is(nesting))
                .SelectMany(frame => frame.WhenFrame(viewType, objectType));
        public static IObservable<T> WhenFrame<T>(this IObservable<T> source, Func<Frame,Type> objectType = null,
            Func<Frame,ViewType> viewType = null, Nesting nesting = Nesting.Any) where T:Frame
            => source.Where(frame => frame.Is(nesting))
                .SelectMany(frame => frame.WhenFrame(viewType?.Invoke(frame)??ViewType.Any, objectType?.Invoke(frame)));

        private static IObservable<T> WhenFrame<T>(this T frame,ViewType viewType, Type types) where T : Frame 
            => frame.View != null
                ? frame.Is(viewType) && frame.Is(types) ? frame.ReturnObservable() : Observable.Empty<T>()
                : frame.WhenViewChanged().Where(t => t.frame.Is(viewType) && t.frame.Is(types)).To(frame);

        public static IObservable<Frame> WhenFrame(this XafApplication application)
            => application.WhenFrameViewChanged();
        public static IObservable<Frame> WhenFrame(this XafApplication application, params ViewType[] viewTypes) 
            => application.WhenFrame().WhenFrame(viewTypes);
        public static IObservable<Frame> WhenFrame(this XafApplication application, params string[] viewIds) 
            => application.WhenFrame().WhenFrame(viewIds);
        
        public static IObservable<Frame> WhenFrame(this XafApplication application, Type objectType ,
            ViewType viewType = ViewType.Any, Nesting nesting = Nesting.Any) 
            => application.WhenFrame(_ => objectType,_ => viewType,nesting);
        
        public static IObservable<Frame> WhenFrame(this XafApplication application, Type objectType ,
            params ViewType[] viewTypes) 
            => application.WhenFrame(objectType).WhenFrame(viewTypes);
        
        public static IObservable<Frame> WhenFrame(this XafApplication application, Func<Frame,Type> objectType,
            Func<Frame,ViewType> viewType = null, Nesting nesting = Nesting.Any) 
            => application.WhenFrame().WhenFrame(objectType,viewType,nesting);
        
        static IObservable<Frame> WhenFrameViewChanged(this XafApplication application) 
            => application.WhenFrameCreated().Merge(application.MainWindow.ReturnObservable().WhenNotDefault())
                .WhenViewChanged().Select(tuple => tuple.frame)
                .StartWith(application.MainWindow).WhenNotDefault(frame => frame?.View);
        
        public static IObservable<Frame> WhenFrameViewControls(this XafApplication application) 
            => application.WhenFrame().SelectMany(frame => frame.View.WhenControlsCreated().Select(view => view).To(frame));

        public static IObservable<T> SelectUntilViewClosed<T>(this IObservable<(XafApplication application, DetailViewCreatingEventArgs e)> source, Func<(XafApplication application, DetailViewCreatingEventArgs e), IObservable<T>> selector)  
            => source.SelectMany(t => selector(t).TakeUntil(t.application.WhenViewCreated().Where(view => view.Id==t.e.ViewID).SelectMany(view => view.WhenClosing())));
        
        public static IObservable<(XafApplication application, DetailViewCreatingEventArgs e)> WhenDetailViewCreating(this XafApplication application,params Type[] objectTypes) 
            => application.WhenEvent<DetailViewCreatingEventArgs>(nameof(XafApplication.DetailViewCreating)).InversePair(application)
                .Where(t => !objectTypes.Any() || objectTypes.Contains(application.Model.Views[t.source.ViewID].AsObjectView.ModelClass.TypeInfo.Type));

        public static IObservable<(XafApplication application, DetailViewCreatedEventArgs e)> WhenDetailViewCreated(this XafApplication application,Type objectType) 
            => application.WhenDetailViewCreated().Where(_ => objectType.IsAssignableFrom(_.e.View.ObjectTypeInfo.Type));

        
        public static IObservable<(XafApplication application, ListViewCreatingEventArgs args)> WhenListViewCreating(this IObservable<XafApplication> source,Type objectType=null,bool? isRoot=null) 
            => source.SelectMany(application => application.WhenListViewCreating(objectType,isRoot));

        public static IObservable<(XafApplication application, ListViewCreatingEventArgs e)> WhenListViewCreating(this XafApplication application,Type objectType=null,bool? isRoot=null) 
            => application.WhenEvent<ListViewCreatingEventArgs>(nameof(XafApplication.ListViewCreating))
                .Where(pattern => (!isRoot.HasValue || pattern.IsRoot == isRoot) &&
                                  (objectType == null || objectType.IsAssignableFrom(pattern.CollectionSource.ObjectTypeInfo.Type))).InversePair(application);

        public static IObservable<(XafApplication application, DetailViewCreatedEventArgs e)> WhenDetailViewCreated(this XafApplication application) 
            => application.WhenEvent<DetailViewCreatedEventArgs>(nameof(XafApplication.DetailViewCreated)).InversePair(application);
        
        public static IObservable<DashboardView> WhenDashboardViewCreated(this XafApplication application) 
            => application.WhenEvent<DashboardViewCreatedEventArgs>(nameof(XafApplication.DashboardViewCreated)).Select(e => e.View);

        
        public static IObservable<(ListView listView, XafApplication application)> WhenListViewCreated(this IObservable<XafApplication> source,Type objectType=null) 
            => source.SelectMany(application => application.WhenListViewCreated(objectType).Pair(application));

        public static IObservable<ListView> WhenListViewCreated(this XafApplication application,Type objectType=null) 
            => application.WhenEvent<ListViewCreatedEventArgs>(nameof(XafApplication.ListViewCreated))
                .Select(pattern => pattern.ListView)
                .Where(view => objectType==null||objectType.IsAssignableFrom(view.ObjectTypeInfo.Type))
                .TraceRX(view => view.Id);

        
        public static IObservable<DashboardView> DashboardViewCreated(this IObservable<XafApplication> source) 
            => source.SelectMany(application => application.WhenDashboardViewCreated());

        
        public static IObservable<(XafApplication application, DetailViewCreatedEventArgs e)> WhenDetailViewCreated(this IObservable<XafApplication> source) 
            => source.SelectMany(application => application.WhenDetailViewCreated());

        public static IObservable<ObjectView> WhenObjectViewCreated(this XafApplication application) 
            => application.ReturnObservable().ObjectViewCreated();

        public static IObservable<ObjectView> ObjectViewCreated(this IObservable<XafApplication> source) 
            => source.ViewCreated().OfType<ObjectView>();
        
        public static IObservable<(XafApplication application, ViewCreatingEventArgs e)> WhenObjectViewCreating(this XafApplication application) 
            => application.ReturnObservable().ObjectViewCreating();

        public static IObservable<(XafApplication application, ViewCreatingEventArgs e)> ObjectViewCreating(this IObservable<XafApplication> source) 
            => source.ViewCreating().WhenNotDefault(t => t.application.Model.Views[t.e.ViewID]?.AsObjectView);

        public static IObservable<View> ViewCreated(this IObservable<XafApplication> source) 
            => source.SelectMany(application => application.WhenViewCreated());

        public static IObservable<View> WhenViewCreated(this XafApplication application) 
            => application.WhenEvent<ViewCreatedEventArgs>(nameof(XafApplication.ViewCreated))
                .Select(pattern => pattern.View);
        
        public static IObservable<(XafApplication application, ViewCreatingEventArgs e)> ViewCreating(this IObservable<XafApplication> source) 
            => source.SelectMany(application => application.WhenViewCreating());

        public static IObservable<(XafApplication application, ViewCreatingEventArgs e)> WhenViewCreating(this XafApplication application) 
            => application.WhenEvent<ViewCreatingEventArgs>(nameof(XafApplication.ViewCreating)).InversePair(application);

        public static IObservable<(Frame SourceFrame, Frame TargetFrame)> WhenViewShown(this XafApplication application) 
            => application.WhenEvent<ViewShownEventArgs>(nameof(XafApplication.ViewShown))
                .Select(pattern => (pattern.SourceFrame,pattern.TargetFrame));

        public static IObservable<(XafApplication application, DatabaseVersionMismatchEventArgs e)> AlwaysUpdateOnDatabaseVersionMismatch(this XafApplication application) 
            => application.WhenDatabaseVersionMismatch().Select(tuple => {
                var updater = tuple.e.Updater;
                var isMiddleTier = ((IObjectSpaceProvider) updater.GetFieldValue("objectSpaceProvider")).IsMiddleTier();
                if (!isMiddleTier) {
                    updater.Update();    
                }
                tuple.e.Handled = true;
                return tuple;
            });

        public static IObservable<(XafApplication application, DatabaseVersionMismatchEventArgs e)> WhenDatabaseVersionMismatch(this XafApplication application) 
            => application.WhenEvent<DatabaseVersionMismatchEventArgs>(nameof(XafApplication.DatabaseVersionMismatch)).InversePair(application);


        public static IObservable<SynchronizationContext> WhenSynchronizationContext(this XafApplication application) 
            => application.WhenWindowCreated(true)
                .Select(_ => SynchronizationContext.Current).WhenNotDefault();

        public static IObservable<(XafApplication application, LogonEventArgs e)> WhenLoggedOn(this IObservable<XafApplication> source) 
            => source.SelectMany(application => application.WhenLoggedOn());

        public static IObservable<(XafApplication application, LogonEventArgs e)> WhenLoggedOn(this XafApplication application) 
            => application.WhenEvent<LogonEventArgs>(nameof(XafApplication.LoggedOn)).InversePair(application);

        
        public static IObservable<(XafApplication application, LogonEventArgs e)> WhenLoggingOn(this IObservable<XafApplication> source) 
            => source.SelectMany(application => application.WhenLoggingOn());

        public static IObservable<(XafApplication application, LogonEventArgs e)> WhenLoggingOn(this XafApplication application) 
            => application.WhenEvent<LogonEventArgs>(nameof(XafApplication.LoggingOn)).InversePair(application);
        
        
        public static IObservable<(XafApplication application, LoggingOffEventArgs e)> WhenLoggingOff(this IObservable<XafApplication> source) 
            => source.SelectMany(application => application.WhenLoggingOff());

        public static IObservable<(XafApplication application, LoggingOffEventArgs e)> WhenLoggingOff(this XafApplication application) 
            => application.WhenEvent<LoggingOffEventArgs>(nameof(XafApplication.LoggingOff)).InversePair(application);
        
        
        public static IObservable<XafApplication> WhenLoggedOff(this IObservable<XafApplication> source) 
            => source.SelectMany(application => application.WhenLoggedOff());

        public static IObservable<XafApplication> WhenLoggedOff(this XafApplication application) 
            => application.WhenEvent(nameof(XafApplication.LoggedOff)).To(application);

        public static IObservable<XafApplication> WhenSetupComplete(this XafApplication application,bool emitIfSetupAlready=true) 
            => emitIfSetupAlready && application.MainWindow != null ? application.ReturnObservable()
                : application.WhenEvent(nameof(XafApplication.SetupComplete)).Take(1)
                    .To(application);

        
        public static IObservable<(XafApplication application, CreateCustomModelDifferenceStoreEventArgs e)> WhenCreateCustomModelDifferenceStore(this XafApplication application) 
            => application.WhenEvent<CreateCustomModelDifferenceStoreEventArgs>(nameof(XafApplication.CreateCustomModelDifferenceStore))
                .Select(e => (application,e));

        
        public static IObservable<(XafApplication application, CreateCustomModelDifferenceStoreEventArgs e)> WhenCreateCustomUserModelDifferenceStore(this XafApplication application) 
            => application.WhenEvent<CreateCustomModelDifferenceStoreEventArgs>(nameof(XafApplication.CreateCustomUserModelDifferenceStore))
                .Select(e => (application,e));

        
        public static IObservable<(XafApplication application, SetupEventArgs e)> WhenSettingUp(this XafApplication application) 
            => application.WhenEvent<SetupEventArgs>(nameof(XafApplication.SettingUp)).Select(e => (application,e));

        public static IObservable<ApplicationModulesManager> WhenApplicationModulesManager(this XafApplication application) 
            => RxApp.ApplicationModulesManager.Where(manager => manager.Application() == application);

        public static IObservable<CreateCustomPropertyCollectionSourceEventArgs> WhenCreateCustomPropertyCollectionSource(this XafApplication application) 
            => application.WhenEvent<CreateCustomPropertyCollectionSourceEventArgs>(nameof(XafApplication.CreateCustomPropertyCollectionSource));
        
        
        public static IObservable<DatabaseUpdaterEventArgs> WhenDatabaseUpdaterCreating(this XafApplication application) 
            => application.WhenEvent<DatabaseUpdaterEventArgs>(nameof(XafApplication.DatabaseUpdaterCreating));

        internal static IObservable<Unit> WhenNonPersistentPropertyCollectionSource(this XafApplication application) 
            => application.WhenCreateCustomPropertyCollectionSource()
                .Where(e => e.ObjectSpace is NonPersistentObjectSpace)
                .Select(e => {
                    e.PropertyCollectionSource = e.NewSource();
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
            => Observable.If(() => DesignerOnlyCalculator.IsRunTime,manager.Defer(() => manager.CheckBlazorCore( hostingStartupType, requiredPackage)));

        private static IObservable<Unit> CheckBlazorCore(this ApplicationModulesManager manager, string hostingStartupType, string requiredPackage) 
            => manager.WhereApplication().ToObservable()
                .SelectMany(application => new[] {(hostingStartupType, requiredPackage), ("Xpand.Extensions.Blazor.HostingStartup", "Xpand.Extensions.Blazor")
                }.ToObservable().SelectMany(t => application.CheckBlazor(t.Item1, t.Item2)));


        public static IObservable<Unit> CheckBlazor(this XafApplication xafApplication, string hostingStartupType, string requiredPackage) {
            if (xafApplication.GetPlatform() == Platform.Blazor) {
                var startup = AssemblyExtensions.EntryAssembly.Attributes()
                    .Where(attribute => attribute.IsInstanceOf("Microsoft.AspNetCore.Hosting.HostingStartupAttribute"))
                    .Where(attribute => ((Type) attribute.GetPropertyValue("HostingStartupType")).FullName == hostingStartupType);
                if (!startup.Any()) {
                    throw new InvalidOperationException($"Install the {requiredPackage} package in the front end project and add: [assembly: HostingStartup(typeof({hostingStartupType}))]");
                }
            }
            return Observable.Empty<Unit>();
        }

        public static IObservable<T> ToObjects<T>(this IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> source)
            => source.SelectMany(t => t.objects);
        public static IEnumerable<T> ToObjects<T>(this IEnumerable<(IObjectSpace objectSpace, IEnumerable<T> objects)> source)
            => source.SelectMany(t => t.objects);
        
        public static IObservable<T> ToObjects<T>(this IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> source) 
            => source.SelectMany(t => t.details.Select(t1 => t1.instance));
        public static IObservable<T[]> ToObjectsList<T>(this IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> source) 
            => source.Select(t => t.details.Select(t1 => t1.instance).ToArray());
        
        public static IObservable<T[]> ToObjectsGroup<T>(this IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> source) 
            => source.Select(t => t.details.Select(t1 => t1.instance).ToArray());
        
        [Obsolete]
        public static IObservable<T> ReloadViewUpdatedObject<T>(this XafApplication application,IObservable<T> source)
            => source.Publish(obs => application.WhenFrame(typeof(T))
                .SelectUntilViewClosed(frame => obs.Where(arg => arg.GetType().IsAssignableFrom(frame.View.ObjectTypeInfo.Type)).ObserveOnContext()
                    .Do(_=> frame.View.Refresh())));
        
        public static IObservable<T> RetryWhenLocked<T>(this IObservable<T> source,int count=5) 
            => Observable.Defer(() => source).RetryWithBackoff(count,
                retryOnError: exception => exception is UserFriendlyException userFriendlyException && (userFriendlyException
                    .InnerException?.GetType().InheritsFrom("DevExpress.Xpo.DB.Exceptions.LockingException") ?? false)) ;
        
        public static IObservable<T> ReloadViewUpdatedObject<T>(this IObservable<T> source,XafApplication application) 
            => source.Publish(obs => application.WhenFrame(typeof(T))
                .SelectUntilViewClosed(frame => obs
                    .Where(arg => arg.GetType().IsAssignableFrom(frame.View.ObjectTypeInfo.Type)).ObserveOnContext()
                    .Do(_=> frame.View.ObjectSpace.Refresh())));

        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenUpdated<T>(
            this XafApplication application,Func<T,bool> criteria,params string[] modifiedProperties) where T:class
            => application.WhenObjectSpaceCreated()
                .SelectMany(objectSpace => objectSpace.WhenCommittedDetailed(ObjectModification.Updated,modifiedProperties,criteria).TakeUntil(objectSpace.WhenDisposed()));
        
        public static IObservable<(IObjectSpace objectSpace, T[] objects)> WhenProviderUpdated<T>(
            this XafApplication application,Func<T,bool> criteria,params string[] modifiedProperties) where T:class
            => application.WhenProviderObjectSpaceCreated()
                .SelectMany(objectSpace => objectSpace.WhenCommittedDetailed(ObjectModification.Updated,modifiedProperties,criteria).TakeUntil(objectSpace.WhenDisposed())
                    .Select(t => (t.objectSpace,t.details.Select(t1 => t1.instance).ToArray())));
        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenProviderUpdated<T>(
            this XafApplication application,params string[] modifiedProperties) where T:class
            => application.WhenProviderObjectSpaceCreated()
                .SelectMany(objectSpace => objectSpace.WhenCommittedDetailed<T>(ObjectModification.Updated,modifiedProperties,_ => true).TakeUntil(objectSpace.WhenDisposed()));
        
        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenCommittedDetailed<T>(
            this XafApplication application,ObjectModification objectModification,Func<T,bool> criteria,params string[] modifiedProperties) where T:class
            => application.WhenObjectSpaceCreated()
                .SelectMany(objectSpace => objectSpace.WhenCommittedDetailed(objectModification,modifiedProperties, criteria).TakeUntil(objectSpace.WhenDisposed()));
        
        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenCommittingDetailed<T>(this IObservable<IObjectSpace> source,
            ObjectModification objectModification,Func<T,bool> criteria,params string[] modifiedProperties) where T:class
            => source.SelectMany(objectSpace => 
                objectSpace.WhenCommitingDetailed(false, objectModification,criteria, modifiedProperties).TakeUntil(objectSpace.WhenDisposed()));
        
        public static IObservable<(IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details)> WhenCommittingDetailed(this IObservable<IObjectSpace> source,
            Type objectType,ObjectModification objectModification,Func<object,bool> criteria,params string[] modifiedProperties)
            => source.SelectMany(objectSpace => 
                objectSpace.WhenCommitingDetailed(objectType,objectModification,false,criteria,modifiedProperties));
        
        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenCommittingDetailed<T>(
            this XafApplication application,ObjectModification objectModification,Func<T,bool> criteria,params string[] modifiedProperties) where T:class
            => application.WhenObjectSpaceCreated().WhenCommittingDetailed(objectModification, criteria,modifiedProperties);
        
        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenProviderCommittingDetailed<T>(
            this XafApplication application,ObjectModification objectModification,Func<T,bool> criteria,params string[] modifiedProperties) where T:class
            => application.WhenProviderObjectSpaceCreated().WhenCommittingDetailed(objectModification, criteria,modifiedProperties);

        public static IObservable<(IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details)> WhenProviderCommittingDetailed(
            this XafApplication application,Type objectType,ObjectModification objectModification,Func<object,bool> criteria=null,params string[] modifiedProperties)
            => application.WhenProviderObjectSpaceCreated().WhenCommittingDetailed(objectType,objectModification, criteria,modifiedProperties);
        
        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenCommittingDetailed<T>(
            this XafApplication application,ObjectModification objectModification,params string[] modifiedProperties) where T:class
            => application.WhenCommittingDetailed<T>(objectModification,null,modifiedProperties);
        
        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenProviderCommittingDetailed<T>(
            this XafApplication application,ObjectModification objectModification,params string[] modifiedProperties) where T:class
            => application.WhenCommittingDetailed<T>(objectModification,null,modifiedProperties);
        
        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenCommittedDetailed<T>(
            this XafApplication application,ObjectModification objectModification,params string[] modifiedProperties)where T:class
            => application.WhenCommittedDetailed<T>(objectModification,null,modifiedProperties);

        public static IObservable<(IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details)> WhenProviderCommittedDetailed(
            this XafApplication application,Type objectType,ObjectModification objectModification,Func<object,bool> criteria=null,params string[] modifiedProperties)
            => application.WhenProviderCommittedDetailed(objectType, objectModification,false,criteria,modifiedProperties);
        
        public static IObservable<(IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details)> WhenProviderCommittedDetailed(
            this XafApplication application,Type objectType,ObjectModification objectModification,bool emitUpdatingObjectSpace,Func<object,bool> criteria=null,params string[] modifiedProperties)
            => application.WhenProviderObjectSpaceCreated(emitUpdatingObjectSpace)
                .SelectMany(objectSpace => objectSpace.WhenCommittedDetailed(objectType, objectModification, criteria, modifiedProperties));
        
        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenProviderCommittedDetailed<T>(
            this XafApplication application,ObjectModification objectModification,Func<T,bool> criteria=null,params string[] modifiedProperties) where T:class
            => application.WhenProviderObjectSpaceCreated()
                .SelectMany(objectSpace => objectSpace.WhenCommittedDetailed(objectModification,modifiedProperties, criteria));
        
        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenProviderCommittedDetailed<T>(
            this XafApplication application,ObjectModification objectModification,bool emitUpdatingObjectSpace,Func<T,bool> criteria=null,params string[] modifiedProperties) where T:class
            => application.WhenProviderObjectSpaceCreated(emitUpdatingObjectSpace)
                .SelectMany(objectSpace => objectSpace.WhenCommittedDetailed(objectModification,modifiedProperties, criteria).TakeUntil(objectSpace.WhenDisposed()));
        
        
        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenProviderNewOrUpdatedCommittedDetailed<T>(
            this XafApplication application,Func<T,bool> criteria,params string[] updatedObjectModifiedProperties) where T:class
            => application.WhenProviderCommittedDetailed(ObjectModification.New,criteria)
                .Merge(application.WhenProviderCommittedDetailed(ObjectModification.Updated,criteria,updatedObjectModifiedProperties));
        
        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenNewOrUpdatedCommittedDetailed<T>(
            this XafApplication application,Func<T,bool> criteria,params string[] updatedObjectModifiedProperties)where T:class
            => application.WhenCommittedDetailed(ObjectModification.New,criteria)
                .Merge(application.WhenCommittedDetailed(ObjectModification.Updated,criteria,updatedObjectModifiedProperties));
        
        public static IObservable<T> UseObjectSpace<T>(this XafApplication application,Func<IObjectSpace,IObservable<T>> factory,bool useObjectSpaceProvider=false,[CallerMemberName]string caller="") 
            => Observable.Using(() => application.CreateObjectSpace(useObjectSpaceProvider, typeof(T), caller: caller), factory);
        
        public static IObservable<T> UseObjectSpace<T>(this XafApplication application,string username,Func<IObjectSpace,IObservable<T>> factory,[CallerMemberName]string caller="") 
            => Observable.Using(() => application.CreateAuthenticatedObjectSpace(username), factory);
        
        public static IObservable<T> UseNonSecuredObjectSpace<T>(this XafApplication application,Func<IObjectSpace,IObservable<T>> factory,bool useObjectSpaceProvider=false,[CallerMemberName]string caller="") 
            => Observable.Using(() => application.CreateNonSecuredObjectSpace(typeof(T)), factory);

        public static IObservable<TResult> UseObject<TSource,TResult>(this XafApplication application,TSource instance,Func<TSource,IObservable<TResult>> selector,bool useObjectSpaceProvider=false,[CallerMemberName]string caller="") 
            => application.UseObjectSpace(space => selector(space.GetObjectFromKey(instance)),useObjectSpaceProvider,caller);

        public static IObservable<T2> UseProviderObjectSpace<T,T2>(this XafApplication application,T obj, Func<T, IObservable<T2>> factory, 
            [CallerMemberName] string caller = "") 
            => application.UseProviderObjectSpace(space => {
                obj = space.GetObjectByKey<T>(space.GetKeyValue(obj));
                return factory(obj);
            }, obj.GetType(),caller:caller);

        public static IObservable<T> UseProviderObjectSpace<T>(this XafApplication application,Func<IObjectSpace,IObservable<T>> factory,Type objectType=null,[CallerMemberName]string caller="") {
            var type =objectType?? typeof(T).RealType();
            return Observable.Using(() => application.CreateObjectSpace(true, type,caller:caller), factory);
        }
        public static IObservable<Unit> UseProviderObjectSpace<T>(this XafApplication application,Action<IObjectSpace> factory,[CallerMemberName]string caller="") 
            => application.UseProviderObjectSpace(space => {
                factory(space);
                return Observable.Empty<T>();
            },caller:caller).ToUnit();

        public static IObservable<Unit> UseObjectSpace(this XafApplication application,Action<IObjectSpace> action,bool useObjectSpaceProvider=false) 
            => Observable.Using(() => application.CreateObjectSpace(useObjectSpaceProvider),space => {
                action(space);
                return Observable.Return(Unit.Default);
            });
        public static IObservable<Unit> UseObjectSpace(this XafApplication application,string user,Action<IObjectSpace> action,bool useObjectSpaceProvider=false) 
            => Observable.Using(() => application.CreateAuthenticatedObjectSpace(user),space => {
                action(space);
                return Observable.Return(Unit.Default);
            });
        
        public static IObservable<Unit> UseObjectSpace(this XafApplication application,Type objectType,Action<IObjectSpace> action) 
            => Observable.Using(() => application.CreateObjectSpace(objectType),space => {
                action(space);
                return Observable.Return(Unit.Default);
            });
        
        public static IObservable<T> UseObjectSpace<T>(this XafApplication application,Type objectType,Func<IObjectSpace,T> selector) 
            => Observable.Using(() => application.CreateObjectSpace(objectType),space => selector(space).ReturnObservable());
        

        public static IObservable<T> WhenObject<T>(this XafApplication application,string[] modifiedProperties,Expression<Func<T, bool>> criteriaExpression=null,
            [CallerMemberName] string caller = "")where T:class
            => application.WhenObject(ObjectModification.NewOrUpdated,modifiedProperties,criteriaExpression,caller);
        
        public static IObservable<T> WhenObject<T>(this XafApplication application,Expression<Func<T, bool>> criteriaExpression=null,
            [CallerMemberName] string caller = "")where T:class
            => application.WhenObject(Array.Empty<string>(),criteriaExpression,caller);

        public static IObservable<T> WhenProviderObject<T>(this XafApplication application,string[] modifiedProperties,
            Expression<Func<T, bool>> criteriaExpression = null, [CallerMemberName] string caller = "")where T:class
            => application.WhenProviderObjects(modifiedProperties,criteriaExpression, caller).SelectMany();
        
        public static IObservable<T> WhenProviderObject<T>(this XafApplication application,
            Expression<Func<T, bool>> criteriaExpression = null, [CallerMemberName] string caller = "")where T:class
            => application.WhenProviderObject(Array.Empty<string>(),criteriaExpression,caller);
        
        public static IObservable<T[]> WhenProviderObjects<T>(this XafApplication application,string[] modifiedProperties,Expression<Func<T, bool>> criteriaExpression=null,
            [CallerMemberName] string caller = "")where T:class
            => application.WhenProviderObjects(ObjectModification.NewOrUpdated,modifiedProperties,criteriaExpression,caller);
        public static IObservable<T[]> WhenProviderObjects<T>(this XafApplication application,Expression<Func<T, bool>> criteriaExpression=null,
            [CallerMemberName] string caller = "")where T:class
            => application.WhenProviderObjects(Array.Empty<string>(),criteriaExpression,caller );

        public static IObservable<T> WhenExistingObject<T>(this XafApplication application, Expression<Func<T, bool>> criteriaExpression = null,
            [CallerMemberName] string caller = "") 
            => application.UseObjectSpace(space => space.GetObjectsQuery<T>().Where(criteriaExpression ?? (arg => true)).ToNowObservable(),caller:caller);
        
        public static IObservable<T> WhenExistingObject<T>(this XafApplication application, string criteriaExpression =null,
            [CallerMemberName] string caller = "") 
            => application.WhenExistingObject<T>(CriteriaOperator.Parse(criteriaExpression),caller);
        public static IObservable<T> WhenExistingObject<T>(this XafApplication application, CriteriaOperator criteriaExpression =null,
            [CallerMemberName] string caller = "") 
            => application.UseObjectSpace(space => space.GetObjects<T>(criteriaExpression).ToNowObservable(),caller:caller);

        public static IObservable<T> WhenObject<T>(this XafApplication application,ObjectModification objectModification,string[] modifiedProperties ,Expression<Func<T, bool>> criteriaExpression=null,
            [CallerMemberName] string caller = "")where T:class 
            => application.WhenObject( objectModification, criteriaExpression, modifiedProperties, (criteriaExpression ?? (arg1 => true)).Compile(),application.WhenObjectSpaceCreated(),caller);
        
        public static IObservable<T[]> WhenObjects<T>(this XafApplication application,ObjectModification objectModification,string[] modifiedProperties ,Expression<Func<T, bool>> criteriaExpression=null,
            [CallerMemberName] string caller = "")where T:class 
            => application.WhenObjects(objectModification, criteriaExpression, modifiedProperties, (criteriaExpression ?? (arg1 => true)).Compile(), application.WhenObjectSpaceCreated(),caller);

        public static IObservable<T> WhenObject<T>(this XafApplication application,ObjectModification objectModification ,Expression<Func<T, bool>> criteriaExpression=null,
            [CallerMemberName] string caller = "")where T:class 
            => application.WhenObject( objectModification,Array.Empty<string>(), criteriaExpression, caller);

        public static IObservable<T> WhenProviderObject<T>(this XafApplication application,ObjectModification objectModification,string[] modifiedProperties ,Expression<Func<T, bool>> criteriaExpression=null,
            [CallerMemberName] string caller = "")where T:class 
            => application.WhenObject( objectModification, criteriaExpression, modifiedProperties, (criteriaExpression ?? (arg1 => true)).Compile(),application.WhenProviderObjectSpaceCreated(),caller);
        
        public static IObservable<T> WhenProviderObject<T>(this XafApplication application,ObjectModification objectModification ,Expression<Func<T, bool>> criteriaExpression=null,
            [CallerMemberName] string caller = "")where T:class 
            => application.WhenProviderObject(objectModification,Array.Empty<string>(),criteriaExpression,caller);
        
        public static IObservable<T[]> WhenProviderObjects<T>(this XafApplication application,ObjectModification objectModification ,Expression<Func<T, bool>> criteriaExpression=null,
            [CallerMemberName] string caller = "")where T:class 
            => application.WhenObjects( objectModification, criteriaExpression, Array.Empty<string>(), (criteriaExpression ?? (arg1 => true)).Compile(),application.WhenProviderObjectSpaceCreated(),caller);
        
        public static IObservable<T[]> WhenProviderObjects<T>(this XafApplication application,ObjectModification objectModification,string[] modifiedProperties ,Expression<Func<T, bool>> criteriaExpression=null,
            [CallerMemberName] string caller = "")where T:class 
            => application.WhenObjects( objectModification, criteriaExpression, modifiedProperties, (criteriaExpression ?? (arg1 => true)).Compile(),application.WhenProviderObjectSpaceCreated(),caller);

        private static IObservable<T> WhenObject<T>(this XafApplication application, ObjectModification objectModification,
            Expression<Func<T, bool>> criteriaExpression, string[] modifiedProperties, Func<T, bool> criteria,IObservable<IObjectSpace> spaceSource, [CallerMemberName] string caller = "")where T:class
            => application.WhenObjects(objectModification, criteriaExpression, modifiedProperties, criteria, spaceSource,caller).SelectMany();
        
        private static IObservable<T[]> WhenObjects<T>(this XafApplication application, ObjectModification objectModification,
            Expression<Func<T, bool>> criteriaExpression, string[] modifiedProperties, Func<T, bool> criteria,IObservable<IObjectSpace> spaceSource,
            [CallerMemberName] string caller = "")where T:class 
            => application.UseObjectSpace(space => space.GetObjectsQuery<T>().Where(criteriaExpression ?? (arg => true)).ToArray().ReturnObservable(),caller:caller)
                .Merge(spaceSource.SelectMany(space => space.WhenCommittedDetailed(objectModification,modifiedProperties, criteria)
                    .Select(t => t.details.Select(t1 => t1.instance).ToArray()))).WhenNotEmpty();

        public static IObservable<T> WhenObject<T>(this XafApplication application,ObjectModification objectModification,bool existing,string[] modifiedProperties ,Expression<Func<T, bool>> criteriaExpression=null) where T:class{
            var criteria = (criteriaExpression ?? (arg1 => true)).Compile();
            var whenCommitted = application.WhenObjectSpaceCreated().SelectMany(space => space.WhenCommittedDetailed(objectModification,modifiedProperties, criteria)
                .SelectMany(t => t.details.Select(t1 => t1.instance)));
            var whenExist = application.UseObjectSpace(space => space.GetObjectsQuery<T>().Where(criteriaExpression ?? (arg => true)).ToNowObservable());
            return Observable.If(() => existing,whenCommitted.Merge(whenExist),whenCommitted);
        }
        public static IObservable<T> WhenObject<T>(this XafApplication application,ObjectModification objectModification,bool existing ,Expression<Func<T, bool>> criteriaExpression=null) where T:class 
            => application.WhenObject(objectModification, existing, Array.Empty<string>(), criteriaExpression);

        internal static IObservable<Unit> PopulateAdditionalObjectSpaces(this XafApplication application) 
            => Observable.If(() => ReactiveModule.PopulateAdditionalObjectSpaces,application.WhenObjectSpaceCreated().OfType<NonPersistentObjectSpace>()
                .Merge(application.ObjectSpaceProviders.ToNowObservable().SelectMany(provider => provider.WhenObjectSpaceCreated().OfType<CompositeObjectSpace>()))
                .Do(space => space.PopulateAdditionalObjectSpaces(application))
                .ToUnit());

        public static IObservable<(Frame source, Frame target, T1 sourceObject, T2 targetObject, int targetIndex)> SynchronizeGridListEditor<T1, T2>(
                this IObservable<(Frame source, Frame target, T1 sourceObject, T2 targetObject, int targetIndex)>  source)
            => source.Do(t => {
                var editor = t.target.View.AsListView().Editor;
                var gridView = editor.GetPropertyValue("GridView");
                if (gridView != null) {
                    gridView.CallMethod("ClearSelection");
                    var index = (int)gridView.CallMethod("FindRow", t.targetObject);
                    gridView.CallMethod("SelectRow", index);
                    gridView.SetPropertyValue("FocusedRowHandle", index);
                }
            });

        public static IObservable<(Frame source, Frame target, T1 sourceObject, T2 targetObject, int targetIndex)> WhenNestedListViewsSelectionChanged<T1, T2>(
            this XafApplication application, Func<T1, T2, bool> objectSelector, Func<IObservable<Frame>, IObservable<Frame>> sourceSelector = null,
                Func<IObservable<Frame>, IObservable<Frame>> targetSelector = null,Func<T1, object> sourceOrderSelector=null,Func<T2, object> targetOrderSelector=null) 
            => application.WhenFrame().WhenFrame(typeof(T1), ViewType.ListView, Nesting.Nested)
                .Publish(sourceFrame => sourceSelector?.Invoke(sourceFrame) ?? sourceFrame)
                .Zip(application.WhenFrame().WhenFrame(typeof(T2), ViewType.ListView, Nesting.Nested)
                    .Publish(targetFrame => targetSelector?.Invoke(targetFrame) ?? targetFrame))
                .Select(t => (source: t.First, target: t.Second)).SelectMany(t => t.source.View.WhenSelectionChanged()
                    .SelectMany(sourceView => sourceView.SelectedObjects.Cast<T1>().OrderBy(arg =>sourceOrderSelector?.Invoke(arg) ).ToArray().ToNowObservable()
                        .SelectMany(sourceObject => t.target.View.AsListView().CollectionSource.Objects().Cast<T2>()
                            .OrderBy(arg => targetOrderSelector?.Invoke(arg)).ToArray().ToNowObservable()
                            .Select((targetObject, targetIndex) => objectSelector(sourceObject, targetObject)
                                ? (t.source, t.target, sourceObject, targetObject, targetIndex) : default))))
                .WhenNotDefault();
        
        public static IObservable<SimpleActionExecuteEventArgs> WhenListViewProcessSelectedItem<T>(this XafApplication application,Nesting nesting=Nesting.Any,bool handled=true)  
            => application.WhenFrame(typeof(T),ViewType.ListView,nesting)
                .SelectMany(frame => frame.GetController<ListViewProcessCurrentObjectController>().WhenCustomProcessSelectedItem(handled));
        
        public static IObservable<IMemberInfo> IgnoreNonPersistentMembersDataLocking(this ApplicationModulesManager manager,Func<Assembly,bool> filterTypes=null)  
            => manager.WhenCustomizeTypesInfo().DomainComponents().GroupBy(info => info.Type.Assembly).Where(types => filterTypes?.Invoke(types.Key)??true)
                .SelectMany(types => types).SelectMany(info => info.Members).Where(info => !info.IsPersistent)
                .Do(info => info.AddAttribute(new IgnoreDataLockingAttribute()));

        public static IObservable<Unit> EnsureViewTabCaptions(this XafApplication application,params Type[] objectTypes)
            => application.WhenFrameCreated().OfType<Window>()
                .MergeIgnored(window => window.WhenViewRefreshExecuted(_ => window.GetController<WindowTemplateController>().UpdateWindowCaption()))
                .ToController<WindowTemplateController>().SelectMany(controller => controller.WhenCustomizeWindowCaption()
                    .DoWhen(_ => objectTypes.Contains(controller.Frame.View?.ObjectTypeInfo?.Type),e =>  e.WindowCaption.FirstPart = $"{controller.Frame.View?.CurrentObject}")).ToUnit();

        public static IObservable<T[]> CommitChangesSequential<T>(this XafApplication application,
            Func<IObjectSpace, IObservable<T>> commit, Func<Func<IObjectSpace, IObservable<T[]>>, Type, IObservable<T>> objectSpaceSource = null, int retry = 3,
            [CallerMemberName] string caller = "") 
            => application.CommitChangesSequential(typeof(T),commit,objectSpaceSource,retry,caller);

        public static IObservable<T[]> CommitChangesSequential<T>(this XafApplication application, Type objectType,
            Func<IObjectSpace, IObservable<T>> commit, Func<Func<IObjectSpace, IObservable<T[]>>, Type, IObservable<T>> objectSpaceSource = null, int retry = 3,
            [CallerMemberName] string caller = "") 
            => Observable.Using(() => new BehaviorSubject<T[]>(null), subject => {
                CommitChangesSubject.OnNext(() => subject.CommitChangesSequential(application,application.ObjectSpaceSource( objectSpaceSource, caller), objectType, commit, retry, caller));
                return subject.DoNotComplete().WhenNotDefault().Take(1).Select(arg => arg).DoOnComplete(() => {});
            });

        private static Func<Func<IObjectSpace, IObservable<T[]>>, Type, IObservable<T>> ObjectSpaceSource<T>(this XafApplication application,
            Func<Func<IObjectSpace, IObservable<T[]>>, Type, IObservable<T>> objectSpaceSource, string caller)
            => (factory, type) => objectSpaceSource?.Invoke(factory, type) ?? application.UseProviderObjectSpace(factory, type, caller).SelectMany( )
                .TakeUntil(application.WhenDisposed());

        private static IObservable<object> CommitChangesSequential<T>(this IObserver<T[]> observer,
            XafApplication xafApplication,
            Func<Func<IObjectSpace, IObservable<T[]>>, Type, IObservable<T>> objectSpaceSource, Type objectType,
            Func<IObjectSpace, IObservable<T>> commit, int retry, string caller) 
            => objectSpaceSource(space => !xafApplication.IsDisposed() ? space.Commit(commit, caller, observer) : Observable.Empty<T[]>(), objectType)
                .RetryWithBackoff(retry).DoOnError(observer.OnError).Select(_ => default(object));


        private static IObservable<T[]> Commit<T>(this IObjectSpace objectSpace,Func<IObjectSpace, IObservable<T>> commit, string caller, IObserver<T[]> observer) 
            => commit(objectSpace).BufferUntilCompleted()
                .Timeout(TimeSpan.FromMinutes(10))
                .Catch<T[], TimeoutException>(_ => Observable.Throw<T[]>(new TimeoutException(caller)))
                .Do(arg => {
                    objectSpace.CommitChanges();
                    observer.OnNext(arg);
                });
    }


}