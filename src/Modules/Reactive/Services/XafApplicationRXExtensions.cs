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
using System.Threading;
using System.Threading.Tasks;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Core;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.MultiTenancy;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Templates;
using DevExpress.ExpressApp.Utils;
using DevExpress.ExpressApp.ViewVariantsModule;
using DevExpress.Persistent.Base.MultiTenancy;
using Fasterflect;
using Microsoft.Extensions.DependencyInjection;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.ExpressionExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.ErrorHandling;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.TypeExtensions;
using Xpand.Extensions.XAF.ApplicationModulesManagerExtensions;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.CollectionSourceExtensions;
using Xpand.Extensions.XAF.CriteriaOperatorExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ObjectExtensions;
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
    public static partial class XafApplicationRxExtensions {
        
        
        static readonly ISubject<Func<IObservable<object>>> CommitChangesSubject=new Subject<Func<IObservable<object>>>();
        static XafApplicationRxExtensions(){
            CommitChangesSubject.Synchronize()
                .SelectManySequential(func => func().CompleteOnError())
                .Subscribe();
        }

        public static IObservable<T> SelectMany<T>(this XafApplication application, IObservable<T> execute) 
            => application.SelectMany(execute.ToTask);

        public static IObservable<T> SelectMany<T>(this XafApplication application, Func<IObservable<T>> execute) 
            => Observable.Defer(() => application.GetPlatform()==Platform.Web?Observable.Start(execute).Merge().Wait().Observe():Observable.Start(execute).Merge())
	            .Catch<T,InvalidOperationException>(_ => Observable.Empty<T>());
        
	    public static IObservable<T> SelectMany<T>(this XafApplication application, Func<Task<T>> execute) 
            => application.GetPlatform()==Platform.Web?Task.Run(execute).Result.Observe():Observable.FromAsync(execute);
        
        public static IObservable<Unit> LogonUser(this XafApplication application,object userKey) 
            => SecurityExtensions.AuthenticateSubject.Where(t => t.authentication== application.Security.GetPropertyValue("Authentication"))
                .Do(t => t.args.SetInstance(_ => userKey)).SelectMany(_ => application.WhenLoggedOn().Take(1)).ToUnit()
                .Merge(Unit.Default.Observe().Do(_ => application.Logon()).IgnoreElements());

        public static IObservable<TSource> BufferUntilCompatibilityChecked<TSource>(this XafApplication application,IObservable<TSource> source) 
            => source.Buffer(application.WhenCompatibilityChecked().Take(1)).Take(1).SelectMany()
                .Concat(Observable.Defer(() => source));

        public static IObservable<XafApplication> WhenCompatibilityChecked(this XafApplication application) 
            => (bool) application.GetPropertyValue("IsCompatibilityChecked")
                ? application.Observe() : application.WhenObjectSpaceCreated().Take(1)
                    .Select(_ => application);

        
        public static IObservable<XafApplication> WhenModule(this IObservable<XafApplication> source, Type moduleType) 
            => source.Where(a => a.Modules.FindModule(moduleType)!=null);
        
        public static IObservable<T> WhenModule<T>(this object value) => FindModules<T>(value).ToNowObservable();

        public static IEnumerable<T> FindModules<T>(this object value){
            var modules = ((IModelSources)CaptionHelper.ApplicationModel).Modules.ToArray();
            var module = modules.Module(value);
            return value is XafApplication application ? application.Modules.OfType<T>()
                : modules.OfType<T>().Where(@base => @base.GetType() == module?.GetType());
        }

        public static IObservable<(ViewItem item, Frame frame)> WhenViewItemControl(this XafApplication application,Type objectType = null, ViewType viewType = ViewType.Any, Nesting nesting = Nesting.Any)
            => application.WhenControllersActivated( objectType,viewType,nesting)
                .SelectMany(frame => frame.View.WhenItemsChanged(true).ToSecond()
                    .Select(e => e.Item)
                    .SelectMany(item => item.WhenControlCreated(true).To(item))
                    .Select(item => (item,frame)));

        public static IObservable<Frame> WhenControllersActivated(this XafApplication application, Type objectType = null, ViewType viewType = ViewType.Any, Nesting nesting = Nesting.Any) 
            => application.WhenFrameCreated().SelectMany(frame => frame.WhenViewControllersActivated().To(frame)
                .Where(frame1 => frame1.When(objectType)&&frame1.When(viewType)&&frame1.When(nesting)));
        
        public static IObservable<Frame> WhenFrameCreated(this XafApplication application,TemplateContext templateContext=default)
            => application.ProcessEvent<FrameCreatedEventArgs>(nameof(XafApplication.FrameCreated)).Select(e => e.Frame)
                .Where(frame => frame.Application==application&& (templateContext==default ||frame.Context == templateContext));
        public static IObservable<T> WhenFrameCreated<T>(this XafApplication application,Func<Frame,IObservable<T>> resilientSelector,TemplateContext templateContext=default)
            => application.ProcessEvent<FrameCreatedEventArgs>(nameof(XafApplication.FrameCreated)).Select(e => e.Frame)
                .Where(frame => frame.Application==application&& (templateContext==default ||frame.Context == templateContext))
                .SelectManyItemResilient(frame => resilientSelector(frame).TakeUntil(frame.WhenDisposedFrame()));

        private static readonly Subject<GenericEventArgs<XafApplication>> WhenExitingSubject = new();
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static bool Exit(XafApplication __instance) {
            var args = new GenericEventArgs<XafApplication>(__instance);
            WhenExitingSubject.OnNext(args);
            return !args.Handled;
        }

        public static IObservable<GenericEventArgs<XafApplication>> WhenExiting(this XafApplication application)
            => WhenExitingSubject.Where(t => t.Instance==application).Take(1);

        public static IObservable<NestedFrame> WhenNestedFrameCreated(this XafApplication application) 
            => application.WhenFrameCreated().OfType<NestedFrame>();

        public static IObservable<T> ToController<T>(this IObservable<Frame> source) where T : Controller 
            => source.SelectMany(window => window.Controllers.Cast<Controller>())
                .Select(controller => controller).OfType<T>()
                .Select(controller => controller);

        public static IObservable<Controller> ToController(this IObservable<Window> source,params string[] names) 
            => source.SelectMany(window => window.Controllers.Cast<Controller>().Where(controller =>
                names.Contains(controller.Name))).Select(controller => controller);

        public static IObservable<Unit> WhenCancelableActionExecuted(this XafApplication application,
            Func<(ActionBaseEventArgs e,IObservable<Unit> cancelSignal), IObservable<Unit>> executeSelector, params string[] actions)
            => application.WhenCancelableActionExecuted<ActionBaseEventArgs>(executeSelector, actions);
        public static IObservable<Unit> WhenCancelableSimpleActionExecuted(this XafApplication application,
            Func<(SimpleActionExecuteEventArgs e,IObservable<Unit> cancelSignal), IObservable<Unit>> executeSelector, params string[] actions)
            => application.WhenCancelableActionExecuted(executeSelector, actions);
        
        public static IObservable<Unit> WhenCancelableActionExecuted<T>(this XafApplication application,
            Func<(T e,IObservable<Unit> cancelSignal), IObservable<Unit>> executeSelector, params string[] actions) where T : ActionBaseEventArgs
            => application.WhenActionExecuted<T>(actions).Where(e => e.Action.Caption!="Cancel")
                .SelectManySequentialItemResilient(e => {
                    var actionCaption = e.Action.Caption;
                    var paintStyle = e.Action.PaintStyle;
                    if (paintStyle == ActionItemPaintStyle.Image) {
                        e.Action.PaintStyle=ActionItemPaintStyle.CaptionAndImage;
                    }
                    e.Action.Caption = "Cancel";
                    return executeSelector((e,e.Action.WhenExecuted().Where(_ => e.Action.Caption=="Cancel").ToUnit()))
                        .TakeUntil(e.Action.WhenExecuted().Where(_ => e.Action.Caption=="Cancel"))
                        .FinallyOnContext(() => {
                            e.Action.Caption = actionCaption;
                            e.Action.PaintStyle=paintStyle;
                        });
                }).ToUnit();

        
        public static IObservable<T> WhenActionExecuted<T>(this XafApplication application, params string[] actions) where T : ActionBaseEventArgs
            => application.WhenActionExecuted(actions).Cast<T>();
        public static IObservable<ActionBaseEventArgs> WhenActionExecuted(this XafApplication application,params string[] actions) 
            => application.WhenFrameCreated().SelectMany(window => window.Actions(actions)).WhenExecuted();

        public static IObservable<ActionBaseEventArgs> WhenActionExecuteCompleted(this XafApplication application,params string[] actions) 
            => application.WhenFrameCreated().SelectMany(window => window.Actions(actions)).WhenExecuteCompleted();
        
        public static IObservable<T> WhenSingleChoiceActionExecuteConcat<T>(this XafApplication application,Func<SingleChoiceActionExecuteEventArgs,IObservable<T>> selector,params string[] actions)  
            => application.WhenFrameCreated().SelectMany(window => window.Actions(actions).OfType<SingleChoiceAction>().ToObservable()
                .SelectMany(a => a.WhenConcatExecution(selector))) ;
        
        public static IObservable<(TAction action, CancelEventArgs e)> WhenActionExecuting<TController, TAction>(
            this XafApplication application, Func<TController, TAction> action) where TController : Controller where TAction : ActionBase
            => application.WhenFrameCreated().ToController<TController>().SelectMany(controller => {
                var actionInstance = action(controller);
                return actionInstance.WhenExecuting<TAction, (TAction, CancelEventArgs)>(
                    e => (action: actionInstance, e).Observe()
                );
            });        
        public static IObservable<ActionBaseEventArgs> WhenActionExecuteCompleted<TController,TAction>(
            this XafApplication application, Func<TController, TAction> action) where TController : Controller where TAction:ActionBase
            => application.WhenFrameCreated().ToController<TController>().Select(action).WhenExecuteCompleted();

        public static IObservable<Window> WhenMainWindowCreated(this XafApplication application,  bool emitIfMainExists = true) 
            => application.WhenWindowCreated(true, emitIfMainExists);
        
        public static IObservable<Window> WhenWindowCreated(this XafApplication application,bool isMain=false,bool emitIfMainExists=true) {
            var windowCreated = application.WhenFrameCreated().OfType<Window>();
            return (isMain ? emitIfMainExists && application.MainWindow != null ? application.MainWindow.Observe()
                    : windowCreated.WhenMainWindowAvailable().Select(window => window)
                : windowCreated);
        }
        public static IObservable<T> WhenWindowCreated<T>(this XafApplication application,Func<Window,IObservable<T>> resilientSelector,bool isMain=false,bool emitIfMainExists=true) 
            => application.WhenWindowCreated(isMain,emitIfMainExists).SelectManyItemResilient(window => resilientSelector(window).TakeUntil(window.WhenDisposedFrame()));

        private static IObservable<Window> WhenMainWindowAvailable(this IObservable<Window> windowCreated) 
            => windowCreated.When(TemplateContext.ApplicationWindow).TemplateChanged().Cast<Window>()
                .If(window => window.Application.GetPlatform() == Platform.Win, window => window.ProcessEvent("Showing")
                    .SelectMany(_ => 1.Seconds().Interval().TakeUntilDisposed(window.Application).ObserveOnContext()
                        .WhenNotDefault(_ => window.Application?.MainWindow).Take(1))
                    .To(window), window => (window.Application.MainWindow ?? window).Observe())
                .Take(1);

        public static IObservable<Window> WhenPopupWindowCreated(this XafApplication application) 
            => application.WhenFrameCreated(TemplateContext.PopupWindow).Where(frame => frame.Application==application).Cast<Window>();
        
        public static void AddObjectSpaceProvider(this XafApplication application, params IObjectSpaceProvider[] objectSpaceProviders) 
            => application.WhenCreateCustomObjectSpaceProvider()
                .Do(t => {
                    var webAPI = application.WhenWeb().FirstOrDefaultAsync().Wait();
                    if (webAPI != null) {
                        application.AddObjectSpaceProvider(objectSpaceProviders, t, webAPI.GetService<NonPersistentObjectSpaceProvider>());
                    }
                    else {
                        application.AddObjectSpaceProvider(objectSpaceProviders, t);
                    }
                })
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
            
            Type[] parameterTypes = [xpoAssembly.GetType("DevExpress.ExpressApp.Xpo.IXpoDataStoreProvider"), typeof(bool)];
            object[] parameterValues = [dataStoreProvider, true];
            if (application.TypesInfo.XAFVersion() > Version.Parse("19.2.0.0")) {
                parameterTypes = parameterTypes.Concat(typeof(bool).YieldItem()).ToArray();
                parameterValues = parameterValues.Concat(false.YieldItem().Cast<object>()).ToArray();
            }

            var type = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name=="Xpand.Extensions.XAF.Xpo")
                           ?.GetType("Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions.FastObjectSpaceProvider") ??
                       xpoAssembly.GetType("DevExpress.ExpressApp.Xpo.XPObjectSpaceProvider");
            return (IObjectSpaceProvider) type.Constructor(parameterTypes).Invoke(parameterValues);
        }

        public static IObservable<IModelApplication> WhenModelChanged(this XafApplication application) 
            => application.ProcessEvent(nameof(XafApplication.ModelChanged))
                .Select(_ =>application.Model )
                .TraceRX();

        public static IObservable<(XafApplication application, CreateCustomObjectSpaceProviderEventArgs e)> WhenCreateCustomObjectSpaceProvider(this XafApplication application) 
            => application.ProcessEvent<CreateCustomObjectSpaceProviderEventArgs>(nameof(XafApplication.CreateCustomObjectSpaceProvider)).InversePair(application);

        public static IObservable<(XafApplication application, CreateCustomTemplateEventArgs e)> WhenCreateCustomTemplate(this XafApplication application) 
            => application.ProcessEvent<CreateCustomTemplateEventArgs>(nameof(XafApplication.CreateCustomTemplate)).InversePair(application);

        
        public static IObservable<IObjectSpace> WhenObjectSpaceCreated(this IObservable<XafApplication> source) 
            => source.SelectMany(application => application.WhenObjectSpaceCreated());

        public static IObservable<(XafApplication application, NonPersistentObjectSpace ObjectSpace)> WhenNonPersistentObjectSpaceCreated(this XafApplication application)
            => application.WhenObjectSpaceCreated().Where(objectSpace => objectSpace is NonPersistentObjectSpace).Select(objectSpace => (application,(NonPersistentObjectSpace)objectSpace));

        public static IObservable<IObjectSpace> WhenProviderObjectSpaceCreated(this XafApplication application,Func<IObjectSpaceProvider> provider=null) {
            var objectSpaceProvider = provider?.Invoke() ?? application.ObjectSpaceProvider;
            return application.ObjectSpaceProviders.Where(spaceProvider => spaceProvider == objectSpaceProvider)
                .ToNowObservable()
                .SelectMany(spaceProvider => spaceProvider.WhenObjectSpaceCreated());
        }

        public static IObservable<TResult> WhenProviderObjectSpaceCreated<TResult>(this XafApplication application,
            Func<IObjectSpaceProvider, IObjectSpace, IObservable<TResult>> resultSelector,
            bool emitUpdatingObjectSpace, Func<IObjectSpaceProvider, bool> match = null)
            => application.WhenLoggingOn(true).SelectMany(_ => application.ObjectSpaceProviders.ToNowObservable())
                .Merge(application.IsLoggedOn().Observe().WhenDefault().SelectMany(_ => application.ObjectSpaceProviders.ToNowObservable()))
                .Where(provider => match?.Invoke(provider) ?? true)
                .Publish(source => source.WhenProviderObjectSpaceCreated(resultSelector,emitUpdatingObjectSpace,application));

        private static IObservable<TResult> WhenProviderObjectSpaceCreated<TResult>(this IObservable<IObjectSpaceProvider> source,Func<IObjectSpaceProvider, IObjectSpace, IObservable<TResult>> resultSelector, bool emitUpdatingObjectSpace,XafApplication application)
            => application.ObjectSpaceProviders.ToNowObservable()
                .SelectMany(spaceProvider => spaceProvider.WhenProviderObjectSpaceCreated(resultSelector, emitUpdatingObjectSpace)
                    .TakeUntil(application.WhenLoggingOn(true)))
                .Concat(source.SelectMany(spaceProvider => spaceProvider.WhenProviderObjectSpaceCreated(resultSelector, emitUpdatingObjectSpace)));
        private static IObservable<TResult> WhenProviderObjectSpaceCreated<TResult>(this IObjectSpaceProvider spaceProvider,Func<IObjectSpaceProvider, IObjectSpace, IObservable<TResult>> resultSelector, bool emitUpdatingObjectSpace) 
            => spaceProvider.WhenObjectSpaceCreated(emitUpdatingObjectSpace).SelectMany(space => resultSelector?.Invoke(spaceProvider, space));

        public static IObservable<IObjectSpace> WhenProviderObjectSpaceCreated(this XafApplication application,bool emitUpdatingObjectSpace,Func<IObjectSpaceProvider,bool> match=null) 
            => application.WhenProviderObjectSpaceCreated((_, objectSpace) => objectSpace.Observe(),emitUpdatingObjectSpace,match);

        public static IObjectSpace CreateAuthenticatedObjectSpace(this XafApplication application, string userName)  
            => application.ServiceProvider.CreateAuthenticatedObjectSpace(application.Security.UserType,userName);

        public static IObservable<(IObjectSpace objectSpace, CancelEventArgs e)> WhenCommiting(this XafApplication  application)
            => application.WhenObjectSpaceCreated().SelectMany(objectSpace => objectSpace.WhenCommiting().Select(e => (objectSpace,e)));        
        public static IObservable<IObjectSpace> WhenObjectSpaceCreated(this XafApplication application, bool includeNonPersistent = true, bool includeNested = false) 
            => application.ProcessEvent<ObjectSpaceCreatedEventArgs>(nameof(XafApplication.ObjectSpaceCreated))
                .Where(t => (includeNonPersistent || t.ObjectSpace is not NonPersistentObjectSpace) &&
                            (includeNested || t.ObjectSpace is not INestedObjectSpace))
                .Select(t => t.ObjectSpace);

        public static IObservable<INestedObjectSpace> WhenNestedObjectSpaceCreated(this XafApplication application, IObjectSpace parentObjectSpace) 
            => application.WhenObjectSpaceCreated(includeNested:true).OfType<INestedObjectSpace>()
                .TakeUntil(parentObjectSpace.WhenDisposed())
                .Where(space => space.ParentObjectSpace==parentObjectSpace);

        public static IObservable<XafApplication> SetupComplete(this IObservable<XafApplication> source) 
            => source.SelectMany(application => application.WhenSetupComplete());

        
        public static IObservable<ListView> ToListView(this IObservable<(XafApplication application, ListViewCreatedEventArgs e)> source) 
            => source.Select(t => t.e.ListView);

        
        public static IObservable<TView> ToObjectView<TView>(this IObservable<(ObjectView view, EventArgs e)> source) where TView:View 
            => source.Where(t => t.view is TView).Select(t => t.view).Cast<TView>();

        public static IObservable<DetailView> ToDetailView(this IObservable<(XafApplication application, DetailViewCreatedEventArgs e)> source) 
            => source.Select(t => t.e.View);

        
        public static IObservable<Frame> WhenViewOnFrame(this IObservable<XafApplication> source, Type objectType = null,
            ViewType viewType = ViewType.Any, Nesting nesting = Nesting.Any) 
            => source.SelectMany(application => application.WhenViewOnFrame(objectType, viewType, nesting));

        public static IObservable<Frame> WhenViewOnFrame(this XafApplication application,Type objectType=null,ViewType viewType=ViewType.Any,Nesting nesting=Nesting.Any) 
            => application.WhenFrameCreated().TemplateViewChanged()
	            .Where(frame => nesting==Nesting.Any|| frame is NestedFrame&&nesting==Nesting.Nested||!(frame is NestedFrame)&&nesting==Nesting.Root)
                .SelectMany(window => (window.View.Observe().When(objectType, viewType, nesting)).To(window))
                .TraceRX(window => window.View.Id);

        public static IObservable<Frame> WhenFrame(this XafApplication application)
            => application.WhenFrameViewChanged();
        public static IObservable<Frame> WhenFrame(this XafApplication application,params Type[] objectTypes)
            => application.WhenFrame().WhenFrame(objectTypes);
        public static IObservable<Frame> WhenFrame(this XafApplication application, params ViewType[] viewTypes) 
            => application.WhenFrame().WhenFrame(viewTypes);
        
        public static IObservable<Frame> WhenFrame(this XafApplication application, Nesting nesting) 
            => application.WhenFrame().WhenFrame(nesting);
        public static IObservable<Frame> WhenFrame(this XafApplication application, params string[] viewIds) 
            => application.WhenFrame().WhenFrame(viewIds);
        
        public static IObservable<Frame> WhenFrame(this XafApplication application, Type objectType ,
            ViewType viewType = ViewType.Any, Nesting nesting = Nesting.Any) 
            => application.WhenFrame(frame => frame.Observe(),objectType,viewType,nesting);
        public static IObservable<T> WhenFrame<T>(this XafApplication application,Func<Frame,IObservable<T>> resilientSelector, Type objectType ,
            ViewType viewType = ViewType.Any, Nesting nesting = Nesting.Any) 
            => application.WhenFrame(resilientSelector, _ => objectType,_ => viewType,nesting);
        public static IObservable<T> WhenFrame<T>(this XafApplication application,Func<Frame,IObservable<T>> resilientSelector, ViewType viewType , Nesting nesting = Nesting.Any) 
            => application.WhenFrame(resilientSelector, _ => typeof(object),_ => viewType,nesting);
        
        public static IObservable<Frame> WhenFrame(this XafApplication application, Type objectType ,
            params ViewType[] viewTypes) 
            => application.WhenFrame(objectType).WhenFrame(viewTypes);
        
        public static IObservable<Frame> WhenFrame(this XafApplication application, Func<Frame,Type> objectType,
            Func<Frame,ViewType> viewType = null, Nesting nesting = Nesting.Any) 
            => application.WhenFrame().WhenFrame(objectType,viewType,nesting);
        
        public static IObservable<T> WhenFrame<T>(this XafApplication application,Func<Frame,IObservable<T>> resilientSelector, Func<Frame,Type> objectType,
            Func<Frame,ViewType> viewType = null, Nesting nesting = Nesting.Any) 
            => application.WhenFrame().WhenFrame(resilientSelector, objectType,viewType,nesting);
        
        static IObservable<Frame> WhenFrameViewChanged(this XafApplication application) 
            => application.WhenFrameCreated().Merge(application.MainWindow.Observe().WhenNotDefault())
                .WhenViewChanged().Select(tuple => tuple.frame)
                .StartWith(application.MainWindow.Cast<Frame>()).WhenNotDefault(frame => frame?.View);
        
        public static IObservable<Frame> WhenFrameViewControls(this XafApplication application) 
            => application.WhenFrame().SelectMany(frame => frame.View.WhenControlsCreated().Select(view => view).To(frame));

        public static IObservable<T> SelectUntilViewClosed<T>(this IObservable<(XafApplication application, DetailViewCreatingEventArgs e)> source, Func<(XafApplication application, DetailViewCreatingEventArgs e), IObservable<T>> selector)  
            => source.SelectMany(t => selector(t).TakeUntil(t.application.WhenViewCreated().Where(view => view.Id==t.e.ViewID).SelectMany(view => view.WhenClosing())));
        
        public static IObservable<(XafApplication application, DetailViewCreatingEventArgs e)> WhenDetailViewCreating(this XafApplication application,params Type[] objectTypes) 
            => application.ProcessEvent<DetailViewCreatingEventArgs>(nameof(XafApplication.DetailViewCreating)).InversePair(application)
                .Where(t => !objectTypes.Any() || objectTypes.Contains(application.Model.Views[t.source.ViewID].AsObjectView.ModelClass.TypeInfo.Type));

        public static IObservable<(XafApplication application, DetailViewCreatedEventArgs e)> WhenDetailViewCreated(this XafApplication application,Type objectType) 
            => application.WhenDetailViewCreated().Where(t => objectType?.IsAssignableFrom(t.e.View.ObjectTypeInfo.Type)??true);
        public static IObservable<(XafApplication application, DetailViewCreatedEventArgs e)> WhenDetailViewCreated(this XafApplication application,params string[] viewIds) 
            => application.WhenDetailViewCreated().Where(t => viewIds.Any(viewId => t.e.View.Id==viewId));
        
        public static IObservable<(XafApplication application, ListViewCreatingEventArgs args)> WhenListViewCreating(this IObservable<XafApplication> source,Type objectType=null,bool? isRoot=null) 
            => source.SelectMany(application => application.WhenListViewCreating(objectType,isRoot));

        public static IObservable<(XafApplication application, ListViewCreatingEventArgs e)> WhenListViewCreating(this XafApplication application,Type objectType=null,bool? isRoot=null) 
            => application.ProcessEvent<ListViewCreatingEventArgs>(nameof(XafApplication.ListViewCreating))
                .Where(pattern => (!isRoot.HasValue || pattern.IsRoot == isRoot) &&
                                  (objectType == null || objectType.IsAssignableFrom(pattern.CollectionSource.ObjectTypeInfo.Type))).InversePair(application);

        public static IObservable<(XafApplication application, DetailViewCreatedEventArgs e)> WhenDetailViewCreated(this XafApplication application) 
            => application.ProcessEvent<DetailViewCreatedEventArgs>(nameof(XafApplication.DetailViewCreated)).InversePair(application);
        
        public static IObservable<DashboardView> WhenDashboardViewCreated(this XafApplication application) 
            => application.ProcessEvent<DashboardViewCreatedEventArgs>(nameof(XafApplication.DashboardViewCreated)).Select(e => e.View);

        
        public static IObservable<(ListView listView, XafApplication application)> WhenListViewCreated(this IObservable<XafApplication> source,Type objectType=null) 
            => source.SelectMany(application => application.WhenListViewCreated(objectType).Pair(application));

        public static IObservable<ListView> WhenListViewCreated(this XafApplication application,params Type[] objectTypes) 
            => application.ProcessEvent<ListViewCreatedEventArgs>(nameof(XafApplication.ListViewCreated))
                .Select(pattern => pattern.ListView)
                .Where(view => !objectTypes.Any()||objectTypes.Any(type => type.IsAssignableFrom(view.ObjectTypeInfo.Type)))
                .TraceRX(view => view.Id);

        
        public static IObservable<DashboardView> DashboardViewCreated(this IObservable<XafApplication> source) 
            => source.SelectMany(application => application.WhenDashboardViewCreated());

        
        public static IObservable<(XafApplication application, DetailViewCreatedEventArgs e)> WhenDetailViewCreated(this IObservable<XafApplication> source) 
            => source.SelectMany(application => application.WhenDetailViewCreated());

        public static IObservable<ObjectView> WhenObjectViewCreated(this XafApplication application) 
            => application.Observe().ObjectViewCreated();

        public static IObservable<ObjectView> ObjectViewCreated(this IObservable<XafApplication> source) 
            => source.ViewCreated().OfType<ObjectView>();
        
        public static IObservable<(XafApplication application, ViewCreatingEventArgs e)> WhenObjectViewCreating(this XafApplication application) 
            => application.Observe().ObjectViewCreating();

        public static IObservable<(XafApplication application, ViewCreatingEventArgs e)> ObjectViewCreating(this IObservable<XafApplication> source) 
            => source.ViewCreating().WhenNotDefault(t => t.application.Model.Views[t.e.ViewID]?.AsObjectView);

        public static IObservable<View> ViewCreated(this IObservable<XafApplication> source) 
            => source.SelectMany(application => application.WhenViewCreated());

        public static IObservable<View> WhenViewCreated(this XafApplication application) 
            => application.ProcessEvent<ViewCreatedEventArgs>(nameof(XafApplication.ViewCreated))
                .Select(pattern => pattern.View);
        
        public static IObservable<(XafApplication application, ViewCreatingEventArgs e)> ViewCreating(this IObservable<XafApplication> source) 
            => source.SelectMany(application => application.WhenViewCreating());

        public static IObservable<(XafApplication application, ViewCreatingEventArgs e)> WhenViewCreating(this XafApplication application) 
            => application.ProcessEvent<ViewCreatingEventArgs>(nameof(XafApplication.ViewCreating)).InversePair(application);

        public static IObservable<(Frame SourceFrame, Frame TargetFrame)> WhenViewShown(this XafApplication application) 
            => application.ProcessEvent<ViewShownEventArgs>(nameof(XafApplication.ViewShown))
                .Select(pattern => (pattern.SourceFrame,pattern.TargetFrame));

        public static IObservable<(XafApplication application, DatabaseVersionMismatchEventArgs e)> AlwaysUpdateOnDatabaseVersionMismatch(this XafApplication application) 
            => application.WhenDatabaseVersionMismatch().Take(1).Select(tuple => {
                var updater = tuple.e.Updater;
                var isMiddleTier = ((IObjectSpaceProvider) updater.GetFieldValue("objectSpaceProvider")).IsMiddleTier();
                if (!isMiddleTier) {
                    updater.Update();    
                }
                tuple.e.Handled = true;
                return tuple;
            });

        public static IObservable<(XafApplication application, DatabaseVersionMismatchEventArgs e)> WhenDatabaseVersionMismatch(this XafApplication application) 
            => application.ProcessEvent<DatabaseVersionMismatchEventArgs>(nameof(XafApplication.DatabaseVersionMismatch)).InversePair(application);

        public static IObservable<SynchronizationContext> WhenSynchronizationContext(this XafApplication application) 
            => application.WhenWindowCreated(true)
                .Select(_ => SynchronizationContext.Current).WhenNotDefault();

        public static IObservable<(XafApplication application, LogonEventArgs e)> WhenLoggedOn(this IObservable<XafApplication> source) 
            => source.SelectMany(application => application.WhenLoggedOn());

        public static IObservable<(XafApplication application, LogonEventArgs e)> WhenLoggedOn(this XafApplication application) 
            => application.ProcessEvent<LogonEventArgs>(nameof(XafApplication.LoggedOn)).InversePair(application);

        public static IObservable<XafApplication> WhenLoggedOn<TParams>(
            this XafApplication application, string userName, string pass=null) where TParams:IAuthenticationStandardLogonParameters
            => application.WhenFrame(typeof(TParams), ViewType.DetailView).Take(1)
                .Do(frame => {
                    var logonParameters = ((TParams)frame.View.CurrentObject);
                    logonParameters.UserName = userName;
                    logonParameters.Password = pass;
                })
                .ToController<DialogController>().WhenAcceptTriggered(application.WhenLoggedOn().Select(t => t.application));
        
        public static IObservable<XafApplication> WhenLoggedOn(this XafApplication application, string userName, string pass=null) 
            => application.WhenLoggedOn<AuthenticationStandardLogonParameters>(userName,pass);
        
        public static IObservable<(XafApplication application, LogonEventArgs e)> WhenLoggingOn(this IObservable<XafApplication> source) 
            => source.SelectMany(application => application.WhenLoggingOn());

        public static IObservable<(XafApplication application, LogonEventArgs e)> WhenLoggingOn(this XafApplication application,bool emitIfLoggedIn=false) 
            =>emitIfLoggedIn&&application.IsLoggedOn()?(application,new LogonEventArgs(null)).Observe(): application.ProcessEvent<LogonEventArgs>(nameof(XafApplication.LoggingOn)).InversePair(application);
        
        
        public static IObservable<(XafApplication application, LoggingOffEventArgs e)> WhenLoggingOff(this IObservable<XafApplication> source) 
            => source.SelectMany(application => application.WhenLoggingOff());

        public static IObservable<(XafApplication application, LoggingOffEventArgs e)> WhenLoggingOff(this XafApplication application) 
            => application.ProcessEvent<LoggingOffEventArgs>(nameof(XafApplication.LoggingOff)).InversePair(application);
        
        
        public static IObservable<XafApplication> WhenLoggedOff(this IObservable<XafApplication> source) 
            => source.SelectMany(application => application.WhenLoggedOff());

        public static IObservable<XafApplication> WhenLoggedOff(this XafApplication application) 
            => application.ProcessEvent(nameof(XafApplication.LoggedOff)).To(application);

        public static IObservable<XafApplication> WhenSetupComplete(this XafApplication application,bool emitIfSetupAlready=true) 
            => emitIfSetupAlready && application.MainWindow != null ? application.Observe()
                : application.ProcessEvent(nameof(XafApplication.SetupComplete)).Take(1)
                    .To(application);
        public static IObservable<T> WhenSetupComplete<T>(this XafApplication application,Func<XafApplication,IObservable<T>> resilientSelector) 
            => application.ProcessEvent(nameof(XafApplication.SetupComplete)).SelectMany(resilientSelector);

        
        public static IObservable<(XafApplication application, CreateCustomModelDifferenceStoreEventArgs e)> WhenCreateCustomModelDifferenceStore(this XafApplication application) 
            => application.ProcessEvent<CreateCustomModelDifferenceStoreEventArgs>(nameof(XafApplication.CreateCustomModelDifferenceStore))
                .Select(e => (application,e));

        
        public static IObservable<(XafApplication application, CreateCustomModelDifferenceStoreEventArgs e)> WhenCreateCustomUserModelDifferenceStore(this XafApplication application) 
            => application.ProcessEvent<CreateCustomModelDifferenceStoreEventArgs>(nameof(XafApplication.CreateCustomUserModelDifferenceStore))
                .Select(e => (application,e));

        
        public static IObservable<(XafApplication application, SetupEventArgs e)> WhenSettingUp(this XafApplication application) 
            => application.ProcessEvent<SetupEventArgs>(nameof(XafApplication.SettingUp)).Select(e => (application,e));

        public static IObservable<ApplicationModulesManager> WhenApplicationModulesManager(this XafApplication application) 
            => RxApp.ApplicationModulesManager.Where(manager => manager.Application() == application);

        public static IObservable<CreateCustomPropertyCollectionSourceEventArgs> WhenCreateCustomPropertyCollectionSource(this XafApplication application) 
            => application.ProcessEvent<CreateCustomPropertyCollectionSourceEventArgs>(nameof(XafApplication.CreateCustomPropertyCollectionSource));
        
        
        public static IObservable<DatabaseUpdaterEventArgs> WhenDatabaseUpdaterCreating(this XafApplication application) 
            => application.ProcessEvent<DatabaseUpdaterEventArgs>(nameof(XafApplication.DatabaseUpdaterCreating));

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

        // public static IObservable<T> ToObjects<T>(this IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> source)
        //     => source.SelectMany(t => t.objects);
        public static IObservable<T[]> ToObjectsGroup<T>(this IObservable<(IObjectSpace objectSpace, IEnumerable<T> objects)> source)
            => source.Select(t => t.objects.ToArray());
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
            this XafApplication application,ObjectModification objectModification,Func<T,bool> criteria,string[] modifiedProperties) where T:class
            => application.WhenObjectSpaceCreated()
                .SelectMany(objectSpace => objectSpace.WhenCommittedDetailed(objectModification,modifiedProperties, criteria).TakeUntil(objectSpace.WhenDisposed()));
        
        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenCommittedDetailed<T>(
            this XafApplication application,ObjectModification objectModification,Func<T,bool> criteria) where T:class
            => application.WhenCommittedDetailed(objectModification,criteria,[]);
        
        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenCommittingDetailed<T>(this IObservable<IObjectSpace> source,
            ObjectModification objectModification,Func<T,bool> criteria,string[] modifiedProperties) where T:class
            => source.SelectMany(objectSpace => 
                objectSpace.WhenCommitingDetailed(false, objectModification,criteria, modifiedProperties).TakeUntil(objectSpace.WhenDisposed()));
        
        public static IObservable<(IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details)> WhenCommittingDetailed(this IObservable<IObjectSpace> source,
            Type objectType,ObjectModification objectModification,Func<object,bool> criteria,string[] modifiedProperties)
            => source.SelectMany(objectSpace => 
                objectSpace.WhenCommitingDetailed(objectType,objectModification,false,modifiedProperties,criteria));
        
        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenCommittingDetailed<T>(
            this XafApplication application,ObjectModification objectModification,Func<T,bool> criteria,params string[] modifiedProperties) where T:class
            => application.WhenObjectSpaceCreated().WhenCommittingDetailed(objectModification, criteria,modifiedProperties);
        
        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenProviderCommittingDetailed<T>(
            this XafApplication application,ObjectModification objectModification,Func<T,bool> criteria,string[] modifiedProperties) where T:class
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
            this XafApplication application,ObjectModification objectModification,string[] modifiedProperties)where T:class
            => application.WhenCommittedDetailed<T>(objectModification,null,modifiedProperties);
        
        
        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenCommittedDetailed<T>(
            this XafApplication application,ObjectModification objectModification)where T:class
            => application.WhenCommittedDetailed<T>(objectModification,null,[]);

        public static IObservable<(IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details)> WhenProviderCommittedDetailed(
            this XafApplication application,Type objectType,string[] modifiedProperties,ObjectModification objectModification,Func<object,bool> criteria=null)
            => application.WhenProviderCommittedDetailed(objectType, objectModification,false,modifiedProperties,criteria);
        public static IObservable<(IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details)> WhenProviderCommittedDetailed(
            this XafApplication application,Type objectType,ObjectModification objectModification,Func<object,bool> criteria=null)
            => application.WhenProviderCommittedDetailed(objectType, objectModification,false,[],criteria);
        public static IObservable<(IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details)> WhenProviderCommittedDetailed(
            this XafApplication application,Type objectType,ObjectModification objectModification,string[] modifiedProperties,Func<object,bool> criteria=null)
            => application.WhenProviderCommittedDetailed(objectType, objectModification,false,modifiedProperties,criteria);
        
        public static IObservable<(IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details)> WhenProviderCommittedDetailed(
            this XafApplication application,Type objectType,ObjectModification objectModification,bool emitUpdatingObjectSpace,string[] modifiedProperties,Func<object,bool> criteria=null)
            => application.WhenProviderObjectSpaceCreated(emitUpdatingObjectSpace)
                .SelectMany(objectSpace => objectSpace.WhenCommittedDetailed(objectType, objectModification, modifiedProperties,criteria));
        public static IObservable<(IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details)> WhenProviderCommittingDetailed(
            this XafApplication application,Type objectType,ObjectModification objectModification,bool emitUpdatingObjectSpace,string[] modifiedProperties,Func<object,bool> criteria=null)
            => application.WhenProviderObjectSpaceCreated(emitUpdatingObjectSpace)
                .SelectMany(objectSpace => objectSpace.WhenCommitingDetailed(objectType,false, objectModification, modifiedProperties,criteria));
        
        public static IObservable<(IObjectSpace objectSpace, (object instance, ObjectModification modification)[] details)> WhenProviderCommittedDetailed(
            this XafApplication application,Type objectType,ObjectModification objectModification,bool emitUpdatingObjectSpace,Func<object,bool> criteria=null)
            => application.WhenProviderCommittedDetailed(objectType,objectModification,emitUpdatingObjectSpace,[],criteria);
        
        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenProviderCommittedDetailed<T>(
            this XafApplication application,ObjectModification objectModification,bool emitUpdatingObjectSpace,Func<T,bool> criteria=null)
            => application.WhenProviderCommittedDetailed(typeof(T),objectModification,emitUpdatingObjectSpace,o => criteria?.Invoke((T)o)??true)
                .Select(t => (t.objectSpace,t.details.Select(t1 => ((T)t1.instance,t1.modification)).ToArray()));
        
        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenProviderCommittedDetailed<T>(
            this XafApplication application,ObjectModification objectModification,string[] modifiedProperties,Func<T,bool> criteria=null,bool emitUpdatingObjectSpace=false) where T:class
            => application.WhenProviderObjectSpaceCreated(emitUpdatingObjectSpace)
                .SelectMany(objectSpace => objectSpace.WhenCommittedDetailed(objectModification,modifiedProperties,criteria));
        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenProviderCommittedDetailed<T>(
            this XafApplication application,ObjectModification objectModification,Func<T,bool> criteria=null,bool emitUpdatingObjectSpace=false) where T:class 
            => application.WhenProviderCommittedDetailed(objectModification, [], criteria,emitUpdatingObjectSpace);

        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenProviderNewOrUpdatedCommittedDetailed<T>(
            this XafApplication application,Func<T,bool> criteria,string[] updatedObjectModifiedProperties) where T:class
            => application.WhenProviderCommittedDetailed(ObjectModification.New,criteria:criteria,modifiedProperties:[])
                .Merge(application.WhenProviderCommittedDetailed(ObjectModification.Updated,criteria:criteria,modifiedProperties:updatedObjectModifiedProperties));
        
        public static IObservable<(IObjectSpace objectSpace, (T instance, ObjectModification modification)[] details)> WhenNewOrUpdatedCommittedDetailed<T>(
            this XafApplication application,Func<T,bool> criteriaSelector,string[] updatedObjectModifiedProperties)where T:class
            => application.WhenCommittedDetailed(ObjectModification.New,criteriaSelector,[])
                .Merge(application.WhenCommittedDetailed(ObjectModification.Updated,criteria:criteriaSelector,modifiedProperties:updatedObjectModifiedProperties));
        
        public static IObservable<T> UseObjectSpace<T>(this XafApplication application,Func<IObjectSpace,IObservable<T>> factory,bool useObjectSpaceProvider=false) 
            => Observable.Using(() => application.CreateObjectSpace(useObjectSpaceProvider, typeof(T)), factory);
        
        public static IObservable<T> UseObjectSpace<T>(this XafApplication application,string username,Func<IObjectSpace,IObservable<T>> factory) 
            => Observable.Using(() => application.CreateAuthenticatedObjectSpace(username), factory);
        
        public static IObservable<T> UseNonSecuredObjectSpace<T>(this XafApplication application,Func<IObjectSpace,IObservable<T>> factory) 
            => Observable.Using(() => application.CreateNonSecuredObjectSpace(typeof(T)), factory);
        
        
        public static IObservable<TResult> UseObject<TSource, TResult>(this XafApplication application,
            TSource instance, Func<TSource, IObservable<TResult>> selector, bool useObjectSpaceProvider = false)
            => application.Using(() => application.CreateObjectSpace(useObjectSpaceProvider, typeof(TSource)),
                space => selector(space.GetObjectFromKey(instance)));

        public static IObservable<TResult> UseArray<TSource,TResult>(this XafApplication application,TSource[] instance,Func<TSource[],IObservable<TResult>> selector,bool useObjectSpaceProvider=false) 
            => application.UseObjectSpace( space => selector(instance.Select(space.GetObject).ToArray()) ,useObjectSpaceProvider);

        public static IObservable<T2> UseProviderObjectSpace<T,T2>(this XafApplication application,T obj, Func<T, IObservable<T2>> factory) 
            => application.UseProviderObjectSpace(space => {
                obj = space.GetObjectByKey<T>(space.GetKeyValue(obj));
                return factory(obj);
            }, obj.GetType());

        public static IObservable<T> UseProviderObjectSpace<T>(this XafApplication application,Func<IObjectSpace,IObservable<T>> factory,Type objectType=null) {
            if (application.IsDisposed())return Observable.Empty<T>();
            var type =objectType?? typeof(T).RealType();
            return application.Using(() => application.CreateObjectSpace(true, type),factory);
        }
        public static IObservable<Unit> UseProviderObjectSpace<T>(this XafApplication application,Action<IObjectSpace> factory) 
            => application.UseProviderObjectSpace(space => {
                factory(space);
                return Observable.Empty<T>();
            }).ToUnit();

        public static IObservable<Unit> UseObjectSpace(this XafApplication application,Action<IObjectSpace> action,bool useObjectSpaceProvider=false) 
            => Observable.Using(() => application.CreateObjectSpace(useObjectSpaceProvider),space => {
                action(space);
                return Observable.Return(Unit.Default);
            });
        public static IObservable<Unit> UseObjectSpace(this XafApplication application,string user,Action<IObjectSpace> action) 
            => Observable.Using(() => application.CreateAuthenticatedObjectSpace(user),space => {
                action(space);
                return Observable.Return(Unit.Default);
            });
        
        public static IObservable<Unit> UseObjectSpace(this XafApplication application,Type objectType,Func<IObjectSpace,IObservable<Unit>> selector) 
            => Observable.Using(() => application.CreateObjectSpace(objectType),selector);
        
        public static IObservable<T> UseObjectSpace<T>(this XafApplication application,Type objectType,Func<IObjectSpace,T> selector) 
            => Observable.Using(() => application.CreateObjectSpace(objectType),space => selector(space).Observe());
        

        public static IObservable<T> WhenObject<T>(this XafApplication application,string[] modifiedProperties,Expression<Func<T, bool>> criteriaExpression=null)where T:class
            => application.WhenObject(ObjectModification.NewOrUpdated,modifiedProperties,criteriaExpression);
        
        public static IObservable<T> WhenObject<T>(this XafApplication application,Expression<Func<T, bool>> criteriaExpression=null)where T:class
            => application.WhenObject([],criteriaExpression);

        public static IObservable<T> WhenProviderObject<T>(this XafApplication application,string[] modifiedProperties,
            Expression<Func<T, bool>> criteriaExpression = null)where T:class
            => application.WhenProviderObjects(modifiedProperties,criteriaExpression).SelectMany();
        
        public static IObservable<T> WhenProviderObject<T>(this XafApplication application,
            Expression<Func<T, bool>> criteriaExpression = null)where T:class
            => application.WhenProviderObject([],criteriaExpression);
        
        public static IObservable<T[]> WhenProviderObjects<T>(this XafApplication application,string[] modifiedProperties,Expression<Func<T, bool>> criteriaExpression=null)where T:class
            => application.WhenProviderObjects(ObjectModification.NewOrUpdated,modifiedProperties,criteriaExpression);
        
        public static IObservable<T[]> WhenProviderObjects<T>(this XafApplication application,Expression<Func<T, bool>> criteriaExpression=null)where T:class
            => application.WhenProviderObjects([],criteriaExpression );

         
        public static IObservable<T> WhenExistingObject<T>(this XafApplication application, Expression<Func<T, bool>> criteriaExpression = null) 
            => application.TypesInfo.DomainComponents(typeof(T)).ToObservable()
                .SelectMany(type => application.UseProviderObjectSpace(
                    space => space.GetObjects(type,(criteriaExpression ?? (arg => true)).ToCriteria()).Cast<T>().ToNowObservable(), type));

        public static IObservable<T[]> WhenExistingObjects<T>(this XafApplication application, Expression<Func<T, bool>> criteriaExpression = null) 
            => application.WhenExistingObject(criteriaExpression).BufferUntilCompleted().WhenNotEmpty();
        
        public static IObservable<T> WhenExistingObject<T>(this XafApplication application, string criteriaExpression ) 
            => application.WhenExistingObject<T>(CriteriaOperator.Parse(criteriaExpression));
        public static IObservable<T> WhenExistingObject<T>(this XafApplication application,Func<T,IObservable<T>> resilientSelector, CriteriaOperator criteriaExpression )
            =>application.UseObjectSpace(space => space.GetObjects<T>(criteriaExpression).ToNowObservable()
                .SelectManyItemResilient(resilientSelector));
        
        public static IObservable<T> WhenExistingObject<T>(this XafApplication application, CriteriaOperator criteriaExpression ) 
            => application.WhenExistingObject<T>(arg => arg.Observe(),criteriaExpression);
        
        public static IObservable<object> WhenProviderExistingObject(this XafApplication application,Type objectType, CriteriaOperator criteriaExpression =null) 
            => application.UseProviderObjectSpace(space => space.GetObjects(objectType,criteriaExpression).Cast<object>().ToNowObservable(),objectType:objectType);

        public static IObservable<T> WhenObject<T>(this XafApplication application,ObjectModification objectModification,string[] modifiedProperties ,Expression<Func<T, bool>> criteriaExpression=null)where T:class 
            => application.WhenObject( objectModification, criteriaExpression, modifiedProperties, application.WhenObjectSpaceCreated());
        
        public static IObservable<T[]> WhenObjects<T>(this XafApplication application,ObjectModification objectModification,string[] modifiedProperties ,Expression<Func<T, bool>> criteriaExpression=null)where T:class 
            => application.WhenObjects(objectModification, criteriaExpression, modifiedProperties, application.WhenObjectSpaceCreated());

        public static IObservable<T> WhenObject<T>(this XafApplication application,ObjectModification objectModification ,Expression<Func<T, bool>> criteriaExpression=null)where T:class 
            => application.WhenObject( objectModification,[], criteriaExpression);

        public static IObservable<T> WhenProviderObject<T>(this XafApplication application,ObjectModification objectModification,string[] modifiedProperties ,Expression<Func<T, bool>> criteriaExpression=null)where T:class 
            => application.WhenObject( objectModification, criteriaExpression, modifiedProperties, application.WhenProviderObjectSpaceCreated());

        public static IObservable<T> WhenObjectUpdatedOrDeleted<T>(this XafApplication application, T value,
            Expression<Func<T,bool>> criteriaExpression=null) where T : class
            => application.WhenObjectUpdated( value, criteriaExpression)
                .Merge(application.WhenProviderObject(ObjectModification.Deleted,criteriaExpression).WhenDomainComponent(value));

        private static IObservable<T> WhenObjectUpdated<T>(this XafApplication application, T value, Expression<Func<T, bool>> criteriaExpression=null) where T : class 
            => application.WhenObjectUpdated( criteriaExpression)
                .Merge(value.Observe().OfType<IObjectSpaceLink>().Where(link => !link.ObjectSpace.IsDisposed)
                    .SelectMany(link => link.ObjectSpace.WhenCommitted<T>().ToObjects()).Cast<T>())
                .WhenDomainComponent(value);

        public static IObservable<T> WhenDomainComponent<T>(this IObservable<T> source,T value) where T : class
            => source.Where(arg => arg.DCKeyValue().Equals(value.DCKeyValue()));

        public static IObservable<T> WhenObjectUpdated<T>(this XafApplication application, Expression<Func<T, bool>> criteriaExpression=null) where T : class {
            var criteria = criteriaExpression?.Compile()??(_ =>true) ;
            return application.WhenProviderCommitted<T>(ObjectModification.Updated).ToObjects().Select(arg => arg).Where(criteria);
        }

        public static IObservable<T> WhenProviderObject<T>(this XafApplication application,ObjectModification objectModification ,Expression<Func<T, bool>> criteriaExpression=null)where T:class 
            => application.WhenProviderObject(objectModification,[],criteriaExpression);
        
        public static IObservable<T[]> WhenProviderObjects<T>(this XafApplication application,ObjectModification objectModification ,Expression<Func<T, bool>> criteriaExpression=null)where T:class 
            => application.WhenObjects( objectModification, criteriaExpression, [], application.WhenProviderObjectSpaceCreated());
        
        public static IObservable<T[]> WhenProviderObjects<T>(this XafApplication application,ObjectModification objectModification,string[] modifiedProperties ,Expression<Func<T, bool>> criteriaExpression=null)where T:class 
            => application.WhenObjects( objectModification, criteriaExpression, modifiedProperties, application.WhenProviderObjectSpaceCreated());

        private static IObservable<T> WhenObject<T>(this XafApplication application, ObjectModification objectModification,
            Expression<Func<T, bool>> criteriaExpression, string[] modifiedProperties, IObservable<IObjectSpace> spaceSource)where T:class
            => application.WhenObjects(objectModification, criteriaExpression, modifiedProperties, spaceSource).SelectMany();
        
        private static IObservable<T[]> WhenObjects<T>(this XafApplication application, ObjectModification objectModification,
            Expression<Func<T, bool>> criteriaExpression, string[] modifiedProperties,IObservable<IObjectSpace> spaceSource)where T:class
            => application.WhenObjects<T>(objectModification, criteriaExpression.ToCriteria(), modifiedProperties, spaceSource, application.TypesInfo.DomainComponents(typeof(T)));
        
        static IObservable<T[]> WhenObjects<T>(this XafApplication application, ObjectModification objectModification,
            CriteriaOperator criteria, string[] modifiedProperties, IObservable<IObjectSpace> spaceSource,params Type[] types)
            => new Subject<T[]>().Use(subject => subject.Merge(types.ToNowObservable().Where(_ => objectModification != ObjectModification.Deleted)
                .SelectMany(type => application.UseObjectSpace(type, space => {
                    var array = space.GetObjects(type, criteria).Cast<T>().ToArray();
                    subject.OnNext(array);
                    return array;
                }))
                .SelectMany()
                .BufferUntilCompleted()
                .Merge(spaceSource.SelectMany(space => types.ToNowObservable()
                    .SelectMany(type => space.WhenCommittedDetailed(type, objectModification, modifiedProperties,
                            o => space.IsObjectFitForCriteria(criteria, o))
                        .Select(t => {
                            var array = t.details.Select(t1 => t1.instance).Cast<T>().ToArray();
                            subject.OnNext(array);
                            return array;
                        }))))
                .IgnoreElements()))
                .WhenNotEmpty();

        public static IObservable<T> WhenObject<T>(this XafApplication application,ObjectModification objectModification,bool existing,string[] modifiedProperties ,Expression<Func<T, bool>> criteriaExpression=null) where T:class{
            var criteria = (criteriaExpression ?? (arg1 => true)).Compile();
            var whenCommitted = application.WhenObjectSpaceCreated().SelectMany(space => space.WhenCommittedDetailed(objectModification,modifiedProperties, criteria)
                .SelectMany(t => t.details.Select(t1 => t1.instance)));
            var whenExist = application.UseObjectSpace(space => space.GetObjectsQuery<T>().Where(criteriaExpression ?? (arg => true)).ToNowObservable());
            return Observable.If(() => existing,whenCommitted.Merge(whenExist),whenCommitted);
        }
        public static IObservable<T> WhenObject<T>(this XafApplication application,ObjectModification objectModification,bool existing ,Expression<Func<T, bool>> criteriaExpression=null) where T:class 
            => application.WhenObject(objectModification, existing, [], criteriaExpression);

        
        internal static IObservable<Unit> PopulateAdditionalObjectSpaces(this XafApplication application) {
            var objectSpaceProviderService = application.ServiceProvider?.GetRequiredService<IObjectSpaceProviderService>();
            var objectSpaceCustomizerService = application.ServiceProvider?.GetRequiredService<IObjectSpaceCustomizerService>();
            void ApplicationOnObjectSpaceCreated(object sender, ObjectSpaceCreatedEventArgs e) {
                if (e.ObjectSpace is not CompositeObjectSpace { Owner: not CompositeObjectSpace } compositeObjectSpace) return;
                if (objectSpaceProviderService != null) {
                    compositeObjectSpace.PopulateAdditionalObjectSpaces(objectSpaceProviderService, objectSpaceCustomizerService);    
                }
                else {
                    compositeObjectSpace.PopulateAdditionalObjectSpaces(application);
                }
            }
            void ApplicationOnDisposed(object sender, EventArgs e) {
                application.ObjectSpaceCreated -= ApplicationOnObjectSpaceCreated;
                application.Disposed -= ApplicationOnDisposed;
            }
            application.ObjectSpaceCreated += ApplicationOnObjectSpaceCreated;
            application.Disposed += ApplicationOnDisposed;
            return Observable.Empty<Unit>();
        }
        
        public static IObservable<(NestedFrame source, NestedFrame target, T1 sourceObject, T2 targetObject, int targetIndex)> SynchronizeGridListEditorSelection<T1, T2>(
                this IObservable<(NestedFrame source, NestedFrame target, T1 sourceObject, T2 targetObject, int targetIndex)>  source)
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
        
        public static IObservable<Unit> SynchronizeNestedListViewSource<T1, T2>(
            this XafApplication application, Expression<Func<T2, T1>> collection) 
            => application.WhenNestedListViewsSelectionChanged(typeof(T1),typeof(T2), collection.MemberExpressionName(), whenSourceViewSelectionChanged:
                    t => t.target.View.ToListView().Observe().Do(view => view.CollectionSource.Criteria[nameof(SynchronizeNestedListViewSource)]=null))
                .Do(t => t.targetFrame.View.ToListView().CollectionSource.Criteria[nameof(SynchronizeNestedListViewSource)] =
                    CriteriaOperator.Parse($"{collection.MemberExpressionName()}=?", collection.Compile()((T2)t.targetObject)))
                .ToUnit();
        public static IObservable<Unit> SynchronizeNestedListViewSource(
            this XafApplication application,Type sourceType,Type targetType, string targetPropertyName) 
            => application.WhenNestedListViewsSelectionChanged(sourceType, targetType, targetPropertyName, whenSourceViewSelectionChanged:
                    t => t.target.View.ToListView().Observe().Do(view => view.CollectionSource.Criteria[nameof(SynchronizeNestedListViewSource)]=null))
                .Do(t => t.targetFrame.View.ToListView().CollectionSource.Criteria[nameof(SynchronizeNestedListViewSource)] =
                    CriteriaOperator.Parse($"{targetPropertyName}=?", t.targetObject.GetTypeInfo().FindMember(targetPropertyName).GetValue(t.targetObject)))
                .ToUnit();
        public static IObservable<Unit> SynchronizeNestedListViewSource(
            this XafApplication application,IMemberInfo sourceMember,IMemberInfo targetMember, string targetPropertyName) 
            => application.WhenNestedListViewsSelectionChanged(sourceMember.ListElementType, targetMember.ListElementType, targetPropertyName
                    ,sourceSelector:sourceFrame =>sourceFrame.Where(frame => frame.ViewItem is ListPropertyEditor editor&&editor.MemberInfo==sourceMember) 
                    ,targetSelector:targetFrame =>targetFrame.Where(frame => frame.ViewItem is ListPropertyEditor editor&&editor.MemberInfo==targetMember) 
                    , whenSourceViewSelectionChanged: t => t.target.View.ToListView().Observe().Do(view => view.CollectionSource.Criteria[nameof(SynchronizeNestedListViewSource)]=null))
                .Do(t => t.targetFrame.View.ToListView().CollectionSource.Criteria[nameof(SynchronizeNestedListViewSource)] =
                    CriteriaOperator.Parse($"{targetPropertyName}=?", t.targetObject.GetTypeInfo().FindMember(targetPropertyName).GetValue(t.targetObject)))
                .ToUnit();

        public static IObservable<(NestedFrame sourceframe, NestedFrame targetFrame, object sourceObject, object targetObject, int targetIndex)> WhenNestedListViewsSelectionChanged(
            this XafApplication application,Type sourceType,Type targetType, string targetPropertyName, Func<IObservable<NestedFrame>, IObservable<NestedFrame>> sourceSelector = null,
                Func<IObservable<NestedFrame>, IObservable<NestedFrame>> targetSelector = null,Func<object, object> sourceSortSelector=null,Func<object, object> targetSortSelector=null,
            Func<(Frame source,Frame target),IObservable<object>> whenSourceViewSelectionChanged=null) 
            => application.WhenNestedListViewsSelectionChanged( sourceType, targetType, (sourceObject, targetObject) => targetObject.GetTypeInfo().FindMember(targetPropertyName).GetValue(targetObject)
                .Equals(sourceObject), sourceSelector, targetSelector, sourceSortSelector, targetSortSelector, whenSourceViewSelectionChanged);

        private static IObservable<(NestedFrame sourceFrame, NestedFrame targetframe, object sourceObject, object targetObject, int
                targetIndex)> WhenNestedListViewsSelectionChanged(this XafApplication application, Type sourceType, Type targetType,
                Func<object,object,bool> match, Func<IObservable<NestedFrame>, IObservable<NestedFrame>> sourceSelector,
                Func<IObservable<NestedFrame>, IObservable<NestedFrame>> targetSelector,
                Func<object, object> sourceSortSelector, Func<object, object> targetSortSelector,
                Func<(Frame source, Frame target), IObservable<object>> whenSourceViewSelectionChanged) 
            => application.WhenFrame(sourceType, ViewType.ListView, Nesting.Nested).Cast<NestedFrame>()
                .Publish(sourceFrame => (sourceSelector?.Invoke(sourceFrame) ?? sourceFrame).Select(frame => frame))
                .Zip(application.WhenFrame(targetType, ViewType.ListView, Nesting.Nested).Cast<NestedFrame>()
                    .Publish(targetFrame => (targetSelector?.Invoke(targetFrame) ?? targetFrame).Select(frame => frame)))
                .Select(t => (source: t.First, target: t.Second)).SelectMany(t => t.source.View.WhenSelectionChanged().StartWith(t.source.View)
                    .ConcatIgnored(_ => whenSourceViewSelectionChanged?.Invoke((t.source,t.target))??Observable.Empty<object>())
                    .SelectMany(sourceView => sourceView.SelectedObjects.Cast<object>().OrderBy(arg =>sourceSortSelector?.Invoke(arg) ).ToArray().ToNowObservable()
                        .SelectMany(sourceObject => t.target.View.AsListView().CollectionSource.Objects()
                            .OrderBy(arg => targetSortSelector?.Invoke(arg)).ToArray().ToNowObservable()
                            .Select((targetObject, targetIndex) => match(sourceObject,targetObject)
                                ? (t.source, t.target, sourceObject, targetObject, targetIndex) : default))))
                .WhenNotDefault();

        public static IObservable<(NestedFrame sourceframe, NestedFrame targetframe, TSource sourceObject, TTarget targetObject, int targetIndex)> WhenNestedListViewsSelectionChanged<TSource,TTarget>(
            this XafApplication application, Expression<Func<TTarget,object>> targetProperty, Func<IObservable<NestedFrame>, IObservable<NestedFrame>> sourceSelector = null,
                Func<IObservable<NestedFrame>, IObservable<NestedFrame>> targetSelector = null,Func<object, object> sourceSortSelector=null,Func<object, object> targetSortSelector=null,
            Func<(Frame source,Frame target),IObservable<object>> whenSourceViewSelectionChanged=null) 
            => application.WhenNestedListViewsSelectionChanged(typeof(TSource), typeof(TTarget),
                targetProperty.MemberExpressionName(), sourceSelector, targetSelector, sourceSortSelector, targetSortSelector, whenSourceViewSelectionChanged)
                .Select(t =>(t.sourceframe,t.targetFrame,(TSource)t.sourceObject,(TTarget)t.targetObject,t.targetIndex) );
        
        public static IObservable<(NestedFrame sourceframe, NestedFrame targetFrame, TSource sourceObject, TTarget targetObject, int targetIndex)> WhenNestedListViewsSelectionChanged<TSource,TTarget>(
            this XafApplication application, Func<TSource,TTarget,bool> match, Func<IObservable<NestedFrame>, IObservable<NestedFrame>> sourceSelector = null,
                Func<IObservable<NestedFrame>, IObservable<NestedFrame>> targetSelector = null,Func<TSource, object> sourceSortSelector=null,Func<TTarget, object> targetSortSelector=null,
            Func<(Frame source,Frame target),IObservable<object>> whenSourceViewSelectionChanged=null) 
            => application.WhenNestedListViewsSelectionChanged(typeof(TSource), typeof(TTarget),
                (sourceObject, targetObject) => match((TSource)sourceObject, (TTarget)targetObject), sourceSelector,
                targetSelector,sourceObject =>sourceSortSelector!((TSource)sourceObject)  , targetObject => targetSortSelector!((TTarget)targetObject), whenSourceViewSelectionChanged)
                .Select(t => (t.sourceFrame,t.targetframe,(TSource)t.sourceObject,(TTarget)t.targetObject,t.targetIndex));
        
        public static IObservable<View> RootView(this XafApplication application,Type objectType,params ViewType[] viewTypes) 
            => application.RootFrame(objectType,viewTypes).Select(frame => frame.View);
        
        public static IObservable<Frame> RootFrame(this XafApplication application,Type objectType,params ViewType[] viewTypes) 
            => application.WhenFrame(objectType,viewTypes).When(TemplateContext.View);

        public static IObservable<Unit> SaveNewObject(this XafApplication application)
            => application.NewObjectRootFrame()
                .SelectMany(frame => frame.View.ToDetailView().CloneRequiredMembers().ToNowObservable()
                    .ConcatToUnit(frame.GetController<ModificationsController>().SaveAction.Observe().Do(action => action.DoExecute())
                        .Select(action => action)));
        
        public static IObservable<bool> DeleteCurrentObject(this XafApplication application)
            => application.NewObjectRootFrame().SelectMany(frame => frame.View.ObjectSpace.WhenCommitted<object>(ObjectModification.New)
                .WaitUntilInactive(1.Seconds()).ObserveOnContext()
                .Select(_ => {
                    var keyValue = frame.View.ObjectSpace.GetKeyValue(frame.View.CurrentObject);
                    var type = frame.View.ObjectTypeInfo.Type;
                    var deleteObjectsViewController = frame.GetController<DeleteObjectsViewController>();
                    deleteObjectsViewController.DeleteAction.ConfirmationMessage = null;
                    deleteObjectsViewController.DeleteAction.DoExecute();
                    return application.CreateObjectSpace(type).GetObjectByKey(type, keyValue)==null;
                }).WhenNotDefault());


        public static IObservable<DetailView> ExistingObjectRootDetailView(this XafApplication application,Type objectType=null)
            => application.RootDetailView(objectType).Where(detailView => !detailView.IsNewObject());

        public static IObservable<DetailView> RootDetailView(this XafApplication application, Type objectType=null) 
            => application.RootFrame(objectType,ViewType.DetailView).Select(frame => frame.View).Cast<DetailView>();
        
        public static IObservable<Frame> RootFrame(this XafApplication application, Type objectType=null) 
            => application.RootFrame(objectType,ViewType.DetailView).WhenNotDefault(frame => frame.View.CurrentObject);

        public static IObservable<DetailView> NewObjectRootDetailView(this XafApplication application,Type objectType)
            => application.NewObjectRootFrame(objectType).Select(frame => frame.View.ToDetailView());
        
        public static IObservable<Frame> NewObjectRootFrame(this XafApplication application,Type objectType=null)
            => application.RootFrame(objectType).Where(frame => frame.View.ToCompositeView().IsNewObject());
        
        public static IObservable<Frame> WhenCustomHandleListViewProcessSelectedItem<T>(this XafApplication application,Nesting nesting=Nesting.Any,bool handled=true)  
            => application.WhenFrame(typeof(T),ViewType.ListView,nesting)
                .SelectMany(frame => frame.GetController<ListViewProcessCurrentObjectController>().WhenCustomHandleProcessSelectedItem(handled).To(frame));
        
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
            Func<IObjectSpace, IObservable<T>> commit, Func<Func<IObjectSpace, IObservable<T[]>>, Type, IObservable<T>> objectSpaceSource = null,bool validate=false, int retry = 3) 
            => application.CommitChangesSequential(typeof(T),commit,objectSpaceSource,validate,retry);
        
        public static IObservable<T[]> CommitChangesSequential<T>(this XafApplication application,
            Func<IObjectSpace, Task<T>> commit, Func<Func<IObjectSpace, IObservable<T[]>>, Type, IObservable<T>> objectSpaceSource = null
            ,bool validate=false, int retry = 3) 
            => application.CommitChangesSequential(typeof(T),space => commit(space).ToObservable(),objectSpaceSource,validate,retry);

        public static IObservable<T[]> CommitChangesSequential<T>(this XafApplication application, Type objectType,
            Func<IObjectSpace, IObservable<T>> commit, Func<Func<IObjectSpace, IObservable<T[]>>, Type, IObservable<T>> objectSpaceSource = null,bool validate=false, int retry = 3) 
            => Observable.Using(() => new BehaviorSubject<T[]>(null), subject => {
                CommitChangesSubject.OnNext(() => subject.CommitChangesSequential(application,application.ObjectSpaceSource( objectSpaceSource), objectType, commit, retry,validate));
                return subject.DoNotComplete().WhenNotDefault()
                    .Merge(application.WhenDisposed().Do(_ => subject.Dispose()).To<T[]>()).Take(1);
            });

        private static Func<Func<IObjectSpace, IObservable<T[]>>, Type, IObservable<T>> ObjectSpaceSource<T>(this XafApplication application,
            Func<Func<IObjectSpace, IObservable<T[]>>, Type, IObservable<T>> objectSpaceSource)
            => (factory, type) => objectSpaceSource?.Invoke(factory, type) ?? application.UseProviderObjectSpace(factory, type).SelectMany( )
                .TakeUntil(application.WhenDisposed());

        private static IObservable<object> CommitChangesSequential<T>(this IObserver<T[]> observer, XafApplication xafApplication,
            Func<Func<IObjectSpace, IObservable<T[]>>, Type, IObservable<T>> objectSpaceSource, Type objectType,
            Func<IObjectSpace, IObservable<T>> commit, int retry, bool validate) 
            => objectSpaceSource(space => !xafApplication.IsDisposed() ? space.Commit(commit, observer,validate) : Observable.Empty<T[]>(), objectType)
                .RetryWithBackoff(retry).DoOnError(observer.OnError).Select(_ => default(object));
        
        public static IObservable<Frame> NavigateBack(this XafApplication application){
            var viewNavigationController = application.MainWindow.GetController<ViewNavigationController>();
            return viewNavigationController.NavigateBackAction
                .Trigger(application.WhenFrame(Nesting.Root).OfType<Window>()
                        .SelectMany(window => window.View.WhenControlsCreated().Take(1).To(window)),
                    () => viewNavigationController.NavigateBackAction.Items.First());
        }
        
        public static IObservable<ListPropertyEditor> WhenNestedFrame(this XafApplication application, Type parentObjectType,params Type[] objectTypes)
            => application.WhenFrame(parentObjectType,ViewType.DetailView).SelectUntilViewClosed(frame => frame.NestedListViews(objectTypes));

        public static IObservable<Frame> Navigate(this XafApplication application, Type objectType,ViewType viewType=ViewType.ListView)
            => application.WhenFrame(objectType,viewType).Publish(frames => application.Navigate(application.FindViewId(viewType, objectType), frames));
        
        public static IObservable<Frame> Navigate(this XafApplication application,string viewId, IObservable<Frame> afterNavigation) 
            => afterNavigation.Publish(source => application.MainWindow == null ? application.WhenWindowCreated(true)
                    .SelectMany(window => window.Navigate(viewId, source))
                : application.MainWindow.Navigate(viewId, source));

        private static IObservable<Frame> Navigate(this Window window,string viewId, IObservable<Frame> afterNavigation){
            var controller = window.GetController<ShowNavigationItemController>();
            return controller.ShowNavigationItemAction.Trigger(afterNavigation,
                () => controller.FindNavigationItemByViewShortcut(new ViewShortcut(viewId, null)));
        }
        public static IObservable<Window> Navigate(this XafApplication application,string viewId,Func<Window,IObservable<Unit>> navigate=null) 
            => application.Navigate(viewId,(frame, id) => frame.WhenFrame(id).Merge(application.WhenFrame(id).Take(1)),navigate).Take(1).Cast<Window>();
        
        public static IObservable<Frame> Navigate(this XafApplication application,string viewId, Func<Frame,string,IObservable<Frame>> afterNavigation,Func<Window,IObservable<Unit>> navigate=null) 
            => application.Defer(() => application.MainWindow == null ? application.WhenWindowCreated(true)
                    .SelectMany(window => window.Navigate(viewId,id => afterNavigation(window,id),navigate))
                : application.MainWindow.Navigate(viewId, id => afterNavigation(application.MainWindow,id),navigate));
        
        public static IObservable<Frame> Navigate(this Window window,string viewId, Func<string,IObservable<Frame>> afterNavigation,Func<Window,IObservable<Unit>> navigate){
            navigate ??= _ => Unit.Default.Observe();
            var controller = window.GetController<ShowNavigationItemController>();
            var item = controller.FindNavigationItemByViewShortcut(new ViewShortcut(viewId, null));
            return navigate(window).SelectMany(_ => controller.ShowNavigationItemAction
                    .Trigger(afterNavigation(((IModelViewVariants)((IModelNavigationItem)item.Model).View).Variants.Current?.View.Id??viewId), () => item));
        }

        private static IObservable<T[]> Commit<T>(this IObjectSpace objectSpace,Func<IObjectSpace, IObservable<T>> commit, IObserver<T[]> observer,bool validate) 
            => commit(objectSpace).BufferUntilCompleted()
                .TakeUntil(_ => objectSpace.IsDisposed)
                .Timeout(TimeSpan.FromMinutes(10))
                .Catch<T[], TimeoutException>(_ => new TimeoutException().Throw<T[]>())
                .Do(arg => {
                    objectSpace.CommitChanges(validate);
                    observer.OnNext(arg);
                });
        
        public static IObservable<TObject[]> LatestProviderObject<TObject, TKey>(this XafApplication application,
            IObservable<TObject> source, Func<TObject, TKey> key, Expression<Func<TObject, bool>> criteria = null)
            where TObject : class {
            var first=new Dictionary<TKey, TObject>();
            var second = application.WhenProviderObjects<TObject>(ObjectModification.NewOrUpdated)
                .Select((objects, i) => {
                    if (i == 0) {
                        first = objects.ToDictionary(key, o => o);
                    }
                    objects.Select(o => first[key(o)] = o).Enumerate();
                    return first;
                });
            var expressionEvaluator = criteria.ExpressionEvaluator();
            return source.CombineLatestWhenFirstEmits(second, (_, objects) => objects.Values.Where(theObject => expressionEvaluator?.Fit(theObject) ?? true).ToArray());
        }
        
        public static IObservable<string> WhenConnectionString(this XafApplication application) 
            => application.GetService<ITenantProvider>().Observe().WhenNotDefault()
                .SelectMany(_ => application.WhenSetupComplete().Take(1))
                .SelectMany(_ => application.WhenObject<ITenantWithConnectionString>(ObjectModification.NewOrUpdated, modifiedProperties:
                        [nameof(ITenantWithConnectionString.ConnectionString)])
                    .Select(tenant =>tenant.ConnectionString ))
                .SwitchIfEmpty(application.Defer(() => application.ObjectSpaceProvider.GetConnectionString().Observe()));
        
    }


}