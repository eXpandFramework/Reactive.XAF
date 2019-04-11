using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using Fasterflect;
using Ryder;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Reactive{

    public static class RxApp{
        static readonly BehaviorSubject<XafApplication> ApplicationSubject=new BehaviorSubject<XafApplication>(null);
        private static readonly MethodInfo CreateNestedFrame;
        private static readonly IObservable<RedirectionContext> WindowsRedirection;
        private static readonly MethodInvoker CreateControllersOptimized;
        private static readonly MethodInvoker CreateControllers;


        internal static IObservable<XafApplication> Connect(this XafApplication application){
            return application.AsObservable()
                .Do(_ => ApplicationSubject.OnNext(_))
                .Select(xafApplication => xafApplication);
        }

        static RxApp(){
            var methodInfos = typeof(XafApplication).Methods();
            CreateControllersOptimized = methodInfos.Where(info => info.Name=="CreateControllers"&&info.Parameters().Count==4).Select(info => info.DelegateForCallMethod()).First();
            CreateControllers = methodInfos.Where(info => info.Name=="CreateControllers"&&info.Parameters().Count==3).Select(info => info.DelegateForCallMethod()).First();
            CreateNestedFrame = methodInfos.First(info => info.Name==nameof(XafApplication.CreateNestedFrame));
            WindowsRedirection = Redirection.Observe(methodInfos.First(info => info.Name=="CreateWindowCore"))
                .Select(context => context)
                .TakeUntilDisposingMainWindow()
                .Publish().RefCount();
//            MainWindowConObs= _applicationSubject.Select(application =>  TemplateContext.ApplicationWindow.Windows().FirstAsync().Cast<Window>()).Concat().LastAsync().Replay();
//            MainWindowConObs.Connect();
        }

        public static IObservable<Unit> UpdateMainWindowStatus<T>(IObservable<T> messages,TimeSpan period=default){
            if (period==default)
                period=TimeSpan.FromSeconds(5);
            return WindowTemplateService.UpdateStatus(period, messages);
        }

        public static IObservable<Frame> Windows(this TemplateContext templateContext){
            
            return Application
                .SelectMany(application => WindowsRedirection
                    .Select(context => {
                        var window = new Window(application, (TemplateContext) context.Arguments[0],(ICollection<Controller>) context.Arguments[1],(bool) context.Arguments[2],(bool) context.Arguments[3]);
                        context.ReturnValue = window;
                        return window;
                    })
                )
                .Where(window => window.Context==templateContext)
                .Publish().AutoConnect();
        }

        public static IObservable<Frame> NestedFrames{
            get{
                return Application
                    .SelectMany(application => Redirection.Observe(CreateNestedFrame)
                        .Select(context => {
                            var controllers = application.OptimizedControllersCreation
                                ? (ICollection<Controller>) CreateControllersOptimized.Invoke(application,
                                    typeof(Controller), true, (ICollection<Controller>) null,
                                    (View) context.Arguments[2])
                                : (ICollection<Controller>) CreateControllers.Invoke(application, typeof(Controller),
                                    true, (ICollection<Controller>) null);

                            var nestedFrame = new NestedFrame(application, (TemplateContext) context.Arguments[1], (ViewItem) context.Arguments[0], controllers);
                            context.ReturnValue = nestedFrame;
                            return nestedFrame;
                        }));
            }
        }

        public static IObservable<TController> GetControllers<TController>(this TemplateContext templateContext) where TController : Controller{
            return Windows(templateContext).Select(frame => frame.GetController<TController>());
        }

        public static IObservable<Frame> Windows(this ActionBase  action) {
            throw new NotImplementedException();   
        }

        public static IObservable<Frame> Windows( ViewType viewType,
            Type objectType = null, Nesting nesting = Nesting.Any, bool? isPopupLookup = null){
            throw new NotImplementedException();   
        }

        public static IObservable<XafApplication> Application => ApplicationSubject.WhenNotDefault();

        public static IObservable<Window> MainWindow => throw new NotImplementedException();

        public static IObservable<(Frame masterFrame, NestedFrame detailFrame)> MasterDetailFrames(Type masterType, Type childType){
            var nestedlListViews = Windows(ViewType.ListView, childType, Nesting.Nested)
                .Select(_ => _)
                .Cast<NestedFrame>();
            return Windows(ViewType.DetailView, masterType)
                .CombineLatest(nestedlListViews.WhenIsNotOnLookupPopupTemplate(),
                    (masterFrame, detailFrame) => (masterFrame, detailFrame))
                .TakeUntilDisposingMainWindow();
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