using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using DevExpress.ExpressApp;
using Fasterflect;
using HarmonyLib;
using Xpand.Extensions.XAF.ApplicationModulesManager;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Security;

namespace Xpand.XAF.Modules.Reactive{
    internal static partial class RxApp{
        
        static readonly Subject<Frame> FramesSubject=new Subject<Frame>();
        static readonly Subject<Window> PopupWindowsSubject=new Subject<Window>();
        static RxApp(){
            Frames = FramesSubject.DistinctUntilChanged()
                .Merge(PopupWindows).Publish();
            ((IConnectableObservable<Frame>) Frames).Connect();
            var harmony = new Harmony(typeof(RxApp).Namespace);
            PatchXafApplication(harmony);

        }

        private static void PatchXafApplication(Harmony harmony){
            var xafApplicationMethods = typeof(XafApplication).Methods();
            var createFrameMethodPatch = GetMethodInfo(nameof(CreateFrame));
            var frameMethods = new[]{
                xafApplicationMethods.First(info => info.Name == nameof(XafApplication.CreateNestedFrame)),
                xafApplicationMethods.First(info => info.Name == nameof(XafApplication.CreateFrame))
            };
            foreach (var frameMethod in frameMethods){
                harmony.Patch(frameMethod, finalizer: new HarmonyMethod(createFrameMethodPatch));
            }

            var createWindows = xafApplicationMethods.Where(info =>
                info.Name == nameof(XafApplication.CreateWindow) );
            foreach (var createWindow in createWindows){
                harmony.Patch(createWindow, finalizer: new HarmonyMethod(GetMethodInfo(nameof(CreateWindow))));    
            }
            

            var createPopupWindow = xafApplicationMethods.First(info => info.Name == nameof(CreatePopupWindow)&&info.Parameters().Count==5);
            harmony.Patch(createPopupWindow, finalizer: new HarmonyMethod(GetMethodInfo(nameof(CreatePopupWindow))));
        }

        private static MethodInfo GetMethodInfo(string methodName){
            return typeof(RxApp).GetMethods(BindingFlags.Static|BindingFlags.NonPublic).First(info => info.Name == methodName);
        }

        private static IObservable<Unit> AddSecuredTypes(this ApplicationModulesManager applicationModulesManager){
            return applicationModulesManager.Modules.OfType<ReactiveModule>().ToObservable()
                .SelectMany(_ => _.ModifyTypesInfo)
                .Select(_ =>_.PersistentTypes.Where(info => info.Attributes.OfType<SecuredTypeAttrbute>().Any())
                        .Select(info => info.Type))
                .Do(infos => {
                    var xafApplication = applicationModulesManager.Application();
                    xafApplication.AddAdditionalSecuredType(infos.ToArray());
                })
                .ToUnit();
        }

        internal static IObservable<Unit> Connect(this ApplicationModulesManager applicationModulesManager){
            
            return applicationModulesManager.AddSecuredTypes()
                ;
        }

//        private static void WebChecks(){
//            var systemWebAssembly = AppDomain.CurrentDomain.GetAssemblies()
//                .FirstOrDefault(assembly => assembly.GetName().Name == "System.Web");
//            var httpContextType = systemWebAssembly?.Types().First(_ => _.Name == "HttpContext");
//            if (httpContextType != null){
//                Frames.OfType<Window>()
//                    .When(TemplateContext.ApplicationWindowContextName)
//                    .TemplateChanged()
//                    .FirstAsync()
//                    .Subscribe(window => {
//                        var isAsync = (bool) window.Template.GetPropertyValue("IsAsync");
//                        if (!isAsync){
//                            var response = httpContextType.GetPropertyValue("Current").GetPropertyValue("Response");
//                            response.CallMethod("Write", "The current page is not async. Add Async=true to page declaration");
//                            response.CallMethod("End");
//                        }
//
//                        var section = ConfigurationManager.GetSection("system.web/httpRuntime");
//                        var values = section.GetPropertyValue("Values");
//                        var indexer = values.GetIndexer("targetFramework");
//                        if (indexer == null || new Version($"{indexer}") < Version.Parse("4.6.1")){
//                            var response = httpContextType.GetPropertyValue("Current").GetPropertyValue("Response");
//                            var message = @"The HttpRuntime use a SynchronizationContext not optimized for asynchronous pages. Please modify your web.config as: <httpRuntime requestValidationMode=""4.5"" targetFramework=""4.6.1"" />";
//                            response.CallMethod("Write",SecurityElement.Escape(message));
//                            response.CallMethod("End");
//                        }
//                    });
//            }
//        }

        public static IObservable<Unit> UpdateMainWindowStatus<T>(IObservable<T> messages,TimeSpan period=default){
            if (period==default)
                period=TimeSpan.FromSeconds(5);
            return WindowTemplateService.UpdateStatus(period, messages);
        }

        internal static IObservable<Window> PopupWindows => PopupWindowsSubject;
        
        internal static IObservable<Frame> Frames{ get; }

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