using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.XAF.Modules.Reactive.Controllers;
using DevExpress.XAF.Modules.Reactive.Extensions;
using DevExpress.XAF.Modules.Reactive.Services;

namespace DevExpress.XAF.Modules.Reactive.Services{

    public static class RxApp{
        static readonly Subject<XafApplication> AppChanged=new Subject<XafApplication>();
        private static XafApplication _xafApplication;
        private static readonly IConnectableObservable<Window> MainWindowConObs;

        static RxApp(){
            AppChanged.Distinct().Select(application => application.WhenDisposed().FirstAsync()).Switch()
                .Subscribe(tuple => { Reset(); });
            MainWindowConObs= AppChanged.Select(application =>  TemplateContext.ApplicationWindow.Frames().FirstAsync().Cast<Window>()).Switch().LastAsync().Replay();
            MainWindowConObs.Connect();
        }

        public static void Reset(){
            RegisterActionsViewController.Reset();
            RegisterActionsWindowController.Reset();
        }

        public static IObservable<Unit> UpdateMainWindowStatus<T>(IObservable<T> messages,TimeSpan period=default){
            if (period==default)
                period=TimeSpan.FromSeconds(5);
            return WindowTemplateService.UpdateStatus(period, messages);
        }

        internal static IObservable<Frame> FrameAssignedToController => RegisterActionsWindowController.WhenFrameAssigned.TakeUntil(XafApplication.WhenDisposed());

        public static IObservable<Frame> Frames(this TemplateContext templateContext){
            return FrameAssignedToController.Where(frame => frame.Context == templateContext);
        }

        public static IObservable<TController> GetControllers<TController>(this TemplateContext templateContext) where TController : Controller{
            return Frames(templateContext).Select(frame => frame.GetController<TController>());
        }

        public static IObservable<Frame> Frames(this ActionBase  action) {
            return FrameAssignedToController.WhenFits(action);
        }

        public static IObservable<Frame> Frames( ViewType viewType,
            Type objectType = null, Nesting nesting = Nesting.Any, bool? isPopupLookup = null){
            return FrameAssignedToController.WhenFits(viewType, objectType, nesting, isPopupLookup);
        }

        

        
        public static XafApplication XafApplication{
            get => _xafApplication;
            internal set{
                _xafApplication = value;
                AppChanged.OnNext(value);
            }
        }

        public static void RegisterViewAction(Func<RegisterActionsViewController,ActionBase[]>actions){
            RegisterActionsViewController.RegisterAction(actions);
        }

        public static void RegisterWindowAction(Func<RegisterActionsWindowController,ActionBase[]>actions){
            RegisterActionsWindowController.RegisterAction(actions);
        }

        public static IObservable<EventPattern<LogonEventArgs>> LoggedOn => Observable
            .FromEventPattern<EventHandler<LogonEventArgs>, LogonEventArgs>(h => XafApplication.LoggedOn += h,
                h => XafApplication.LoggedOn -= h).Select(pattern => pattern);


        public static IObservable<View> ViewCreated => Observable
            .FromEventPattern<EventHandler<ViewCreatedEventArgs>, ViewCreatedEventArgs>(h => XafApplication.ViewCreated += h,
                h => XafApplication.ViewCreated -= h)
            .Select(pattern => pattern.EventArgs.View)
            .TakeUntil(XafApplication.WhenDisposed());

        public static IObservable<Window> MainWindow => MainWindowConObs;

        public static IObservable<(Frame masterFrame, NestedFrame detailFrame)> MasterDetailFrames(Type masterType, Type childType){
            var nestedlListViews = Frames(ViewType.ListView, childType, Nesting.Nested)
                .Select(_ => _)
                .Cast<NestedFrame>();
            return Frames(ViewType.DetailView, masterType)
                .CombineLatest(nestedlListViews.WhenIsNotOnLookupPopupTemplate(),
                    (masterFrame, detailFrame) => (masterFrame, detailFrame))
                .TakeUntil(XafApplication.WhenDisposed());
        }

        public static IObservable<(Frame masterFrame, NestedFrame detailFrame)> NestedDetailObjectChanged(Type nestedType, Type childType){
            return MasterDetailFrames(nestedType, childType).SelectMany(_ => {
                return _.masterFrame.View.WhenCurrentObjectChanged().Select(tuple => _);
            });
        }

        public static IObservable<(ObjectsGettingEventArgs e,TSignal signals, Frame masterFrame, NestedFrame detailFrame)>
            AddNestedNonPersistentObjects<TSignal>(Type masterObjectType, Type detailObjectType,
                Func<(Frame masterFrame, NestedFrame detailFrame), IObservable<TSignal>> addSignal){

            return Observable.Create<(ObjectsGettingEventArgs e,TSignal signals, Frame masterFrame, NestedFrame detailFrame)>(
                observer => {
                    return NestedDetailObjectChanged(masterObjectType, detailObjectType)
                        .SelectMany(_ => AddNestedNonPersistentObjectsCore(addSignal, _, observer))
                        .Subscribe(response => {},() => {});
                });
        }

        public static IObservable<(ObjectsGettingEventArgs e, TSignal signal, Frame masterFrame, NestedFrame
                detailFrame)>
            AddNestedNonPersistentObjects<TSignal>(this IObservable<(Frame masterFrame,NestedFrame detailFrame)> source,
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