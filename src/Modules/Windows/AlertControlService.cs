using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Utils;
using DevExpress.XtraBars.Alerter;
using Fasterflect;
using HarmonyLib;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.Harmony;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.Windows{
    static class AlertControlService {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static void Show(AlertControl __instance, Form owner, AlertInfo info) {
            var modelWindows = CaptionHelper.ApplicationModel.ToReactiveModule<IModelReactiveModuleWindows>().Windows;
            __instance.FormLocation = modelWindows.Alert.FormLocation;
            if (modelWindows.Alert.FormWidth != null) {
                __instance.WhenEvent<AlertFormWidthEventArgs>(nameof(AlertControl.GetDesiredAlertFormWidth)).FirstAsync()
                    .Subscribe(e => e.Width = modelWindows.Alert.FormWidth.Value);
            }
            
        }

        static AlertControlService() 
            => new HarmonyMethod(typeof(AlertControlService), nameof(Show))
                .PreFix(typeof(AlertControl).Method(nameof(AlertControl.Show), new[] { typeof(Form), typeof(AlertInfo) }),true);

        public static IObservable<Unit> ConnectAlertForm(this ApplicationModulesManager manager) => Observable.Empty<Unit>();
    }
}