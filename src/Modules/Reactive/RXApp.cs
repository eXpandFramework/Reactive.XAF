using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Security;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Fasterflect;
using Ryder;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Reactive{

    public static class RxApp{
        static readonly ISubject<XafApplication> ApplicationSubject=Subject.Synchronize(new BehaviorSubject<XafApplication>(null));
        private static readonly MethodInvoker CreateWindowCore;
        private static readonly IObservable<RedirectionContext> OnPopupWindowCreated;
        private static readonly MethodInvoker CreateControllersOptimized;
        private static readonly MethodInvoker CreateControllers;
        private static readonly IObservable<RedirectionContext> NestedFrameRedirection;
        private static readonly IObservable<RedirectionContext> FrameRedirection;
        private static readonly IObservable<RedirectionContext> WindowRedirection;


        static RxApp(){
            var methodInfos = typeof(XafApplication).Methods();
            CreateControllersOptimized = methodInfos.Where(info => info.Name==nameof(CreateControllers)&&info.Parameters().Count==4).Select(info => info.DelegateForCallMethod()).First();
            CreateControllers = methodInfos.Where(info => info.Name==nameof(CreateControllers)&&info.Parameters().Count==3).Select(info => info.DelegateForCallMethod()).First();
            var methodInfo = methodInfos.First(info => info.Name == nameof(CreateWindowCore));
            Redirection.Observe(methodInfo)
                .Subscribe(context => { });
            CreateWindowCore = methodInfo.DelegateForCallMethod();
            
            OnPopupWindowCreated = Redirection.Observe(methodInfos.First(info => info.Name==nameof(OnPopupWindowCreated)))
                .Publish().RefCount();

            var createNestedFrame = methodInfos.First(info => info.Name==nameof(XafApplication.CreateNestedFrame));
            NestedFrameRedirection = Redirection.Observe(createNestedFrame)
                .Publish().RefCount();
            var createFrame = methodInfos.First(info => info.Name==nameof(XafApplication.CreateFrame));
            FrameRedirection = Redirection.Observe(createFrame)
                .Publish().RefCount();
            var createWindowMethod = typeof(XafApplication).Methods()
                .First(info =>info.Parameters().Count==5&& info.Name == nameof(XafApplication.CreateWindow));
            var connectableObservable = Redirection.Observe(createWindowMethod).Publish();
            connectableObservable.Connect();
            connectableObservable.Subscribe();
            WindowRedirection=connectableObservable;
//            WindowRedirection.con;
            WebChecks();
        }

        internal static IObservable<Unit> Connect(this XafApplication application){
            return application.AsObservable()
                .Do(_ => ApplicationSubject.OnNext(_))
                .ToUnit()
                .Merge(Windows.ToUnit());
        }

        private static void WebChecks(){
            var systemWebAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetName().Name == "System.Web");
            var httpContextType = systemWebAssembly?.Types().First(_ => _.Name == "HttpContext");
            if (httpContextType != null){
                Windows.When(TemplateContext.ApplicationWindowContextName)
                    .TemplateChanged()
                    .FirstAsync()
                    .Subscribe(window => {
                        var isAsync = (bool) window.Template.GetPropertyValue("IsAsync");
                        if (!isAsync){
                            var response = httpContextType.GetPropertyValue("Current").GetPropertyValue("Response");
                            response.CallMethod("Write", "The current page is not async. Add Async=true to page declaration");
                            response.CallMethod("End");
                        }

                        var section = ConfigurationManager.GetSection("system.web/httpRuntime");
                        var values = section.GetPropertyValue("Values");
                        var indexer = values.GetIndexer("targetFramework");
                        if (indexer == null || new Version($"{indexer}") < Version.Parse("4.6.1")){
                            var response = httpContextType.GetPropertyValue("Current").GetPropertyValue("Response");
                            var message = @"The HttpRuntime use a SynchronizationContext not optimized for asynchronous pages. Please modify your web.config as: <httpRuntime requestValidationMode=""4.5"" targetFramework=""4.6.1"" />";
                            response.CallMethod("Write",SecurityElement.Escape(message));
                            response.CallMethod("End");
                        }
                    });
            }
        }

        public static IObservable<Unit> UpdateMainWindowStatus<T>(IObservable<T> messages,TimeSpan period=default){
            if (period==default)
                period=TimeSpan.FromSeconds(5);
            return WindowTemplateService.UpdateStatus(period, messages);
        }

        public static IObservable<Window> Windows{
            get{
                return WindowRedirection
                    .Select(context => {
                        var t = (templateContext: (TemplateContext) context.Arguments[0],
                            controllers: (ICollection<Controller>) context.Arguments[1],
                            createAllControllers: (bool) context.Arguments[2], isMain: (bool) context.Arguments[3],
                            view: (View) context.Arguments[4], application: (XafApplication) context.Sender);

                        var list = t.application.OptimizedControllersCreation
                            ? CreateControllers(t.application, typeof(Controller), t.createAllControllers, t.controllers,t.view)
                            : CreateControllers(t.application, typeof(Controller), t.createAllControllers, t.controllers);
                        var window = (Window) CreateWindowCore(t.application, t.templateContext, list, t.isMain, true);
                        context.ReturnValue = window;
                        return window;
                    });
            }
        }

        public static IObservable<Frame> Frames{
            get{
                return FrameRedirection
                    .Select(context => {
                        var templateContext = ((TemplateContext) context.Arguments[0]);
                        var controllers = ((ICollection<Controller>) CreateControllers(null,typeof(Controller),true,(ICollection<Controller>)null));
                        return new Frame(null,templateContext,controllers);
                    })
                    .Merge(NestedFrames)
                    .Merge(Windows);
            }
        }

        public static IObservable<NestedFrame> NestedFrames{
            get{
                return NestedFrameRedirection
                    .Select(context => {
                        var viewItem = (ViewItem) context.Arguments[0];
                        var application = (XafApplication)viewItem.View.GetPropertyValue("Application");
                        var controllers = application.OptimizedControllersCreation
                            ? (ICollection<Controller>) CreateControllersOptimized.Invoke(application,
                                typeof(Controller), true, (ICollection<Controller>) null,
                                (View) context.Arguments[2])
                            : (ICollection<Controller>) CreateControllers.Invoke(application, typeof(Controller),
                                true, (ICollection<Controller>) null);
                        var nestedFrame = new NestedFrame(application, (TemplateContext) context.Arguments[1], viewItem, controllers);
                        context.ReturnValue = nestedFrame;
                        return nestedFrame;
                    });
            }
        }

        public static IObservable<XafApplication> Application => ApplicationSubject.WhenNotDefault();

        public static IObservable<Window> MainWindow => throw new NotImplementedException();

        public static IObservable<(Frame masterFrame, NestedFrame detailFrame)> MasterDetailFrames(Type masterType, Type childType){
            throw new NotImplementedException();
//            var nestedlListViews = Windows(ViewType.ListView, childType, Nesting.Nested)
//                .Select(_ => _)
//                .Cast<NestedFrame>();
//            return Windows(ViewType.DetailView, masterType)
//                .CombineLatest(nestedlListViews.WhenIsNotOnLookupPopupTemplate(),
//                    (masterFrame, detailFrame) => (masterFrame, detailFrame))
//                .TakeUntilDisposingMainWindow();
        }

        public static IObservable<(Frame masterFrame, NestedFrame detailFrame)> NestedDetailObjectChanged(Type nestedType, Type childType){
            return MasterDetailFrames(nestedType, childType).SelectMany(_ => {
                return _.masterFrame.View.WhenCurrentObjectChanged().Select(tuple => _);
            });
        }

        public static IObservable<(ObjectsGettingEventArgs e, TSignal signals, Frame masterFrame, NestedFrame detailFrame)> AddNestedNonPersistentObjects<TSignal>(Type masterObjectType, Type detailObjectType,
                Func<(Frame masterFrame, NestedFrame detailFrame), IObservable<TSignal>> addSignal){

            return Observable.Create<(ObjectsGettingEventArgs e,TSignal signals, Frame masterFrame, NestedFrame detailFrame)>(
                observer => {
                    return NestedDetailObjectChanged(masterObjectType, detailObjectType)
                        .SelectMany(_ => AddNestedNonPersistentObjectsCore(addSignal, _, observer))
                        .Subscribe(response => {},() => {});
                });
        }

        public static IObservable<(ObjectsGettingEventArgs e, TSignal signal, Frame masterFrame, NestedFrame detailFrame)> AddNestedNonPersistentObjects<TSignal>(this IObservable<(Frame masterFrame, NestedFrame detailFrame)> source,
                Func<(Frame masterFrame, NestedFrame detailFrame), IObservable<TSignal>> addSignal){

            return source.SelectMany(tuple => {
                return Observable.Create<(ObjectsGettingEventArgs e,TSignal signal, Frame masterFrame, NestedFrame detailFrame)>(
                    observer => AddNestedNonPersistentObjectsCore(addSignal, tuple, observer).Subscribe());

            });
        }

        private static IObservable<TSignal> AddNestedNonPersistentObjectsCore<TSignal>(Func<(Frame masterFrame, NestedFrame detailFrame), IObservable<TSignal>> addSignal,
            (Frame masterFrame, NestedFrame detailFrame) _, IObserver<(ObjectsGettingEventArgs e, TSignal signal, Frame masterFrame, NestedFrame detailFrame)> observer){
            return addSignal(_)
                .When(_.masterFrame, _.detailFrame)
                .Select(signals => {
                    using (var unused = ((NonPersistentObjectSpace) _.detailFrame.View.ObjectSpace)
                        .WhenObjectsGetting()
                        .Do(tuple => observer.OnNext((tuple.e, signals, _.masterFrame, _.detailFrame)),() => {})
                        .Subscribe()){
                        ((ListView) _.detailFrame.View).CollectionSource.ResetCollection();
                    }

                    return signals;
                });
        }
    }

}