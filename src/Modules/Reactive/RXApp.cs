using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using DevExpress.ExpressApp;
using Fasterflect;
using HarmonyLib;
using JetBrains.Annotations;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ApplicationModulesManager;
using Xpand.Extensions.XAF.XafApplication;
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

        private static IObservable<Unit> AddNonSecuredTypes(this ApplicationModulesManager applicationModulesManager){
            return applicationModulesManager.Modules.OfType<ReactiveModule>().ToObservable()
                .SelectMany(_ => _.ModifyTypesInfo)
                .Select(_ =>_.PersistentTypes.Where(info => info.Attributes.OfType<NonSecuredTypeAttrbute>().Any())
                        .Select(info => info.Type))
                .Do(infos => {
                    var xafApplication = applicationModulesManager.Application();
                    xafApplication?.AddNonSecuredType(infos.ToArray());
                })
                .ToUnit();
        }

        internal static IObservable<Unit> Connect(this ApplicationModulesManager applicationModulesManager){
            return applicationModulesManager.AddNonSecuredTypes();
        }
        
        [PublicAPI]
        public static IObservable<Unit> UpdateMainWindowStatus<T>(IObservable<T> messages,TimeSpan period=default){
            if (period==default)
                period=TimeSpan.FromSeconds(5);
            return WindowTemplateService.UpdateStatus(period, messages);
        }

        internal static IObservable<Window> PopupWindows => PopupWindowsSubject;
        
        internal static IObservable<Frame> Frames{ get; }
        
    }

}