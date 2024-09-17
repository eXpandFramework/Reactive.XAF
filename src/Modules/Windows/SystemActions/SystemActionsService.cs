using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Model;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Windows.SystemActions {
    public static class SystemActionsService {
        private static readonly Subject<HotKeyManager> CustomizeHotKeyManagerSubject = new();
        public static IObservable<HotKeyManager> CustomizeHotKeyManager => CustomizeHotKeyManagerSubject.AsObservable();

        internal static IObservable<Unit> SystemActionsConnect(this XafApplication application) 
            => application.WhenHotKeyManager().ExecuteHotKeys(application);

        private static IObservable<Unit> ExecuteHotKeys(this IObservable<(HotKeyManager manager, Frame window)> source, XafApplication application)
            => source.SelectMany(hotKeyManager => application.WhenHotKeyPressed(hotKeyManager.manager)
                .Publish(hotkeyPressed=> application.WhenActionActivated(hotKeyManager)
                    .SelectMany(activated => hotkeyPressed
                        .Where(model => activated.model==model)
                        // .TakeUntil(activated.action.WhenDeactivated())
                        // .DoWhen(action => action is IModelSystemAction { Focus: true },_ => SetForegroundWindow(application.MainWindow.Template.To<Form>().Handle))
                        .SelectAndOmit(model => activated.WaitTheExecute( model)))));

        private static IObservable<Unit> WaitTheExecute(this (ActionBase action, IModelHotkeyAction model) activated, IModelHotkeyAction model) 
            => activated.action.WhenExecuteCompleted().TakeFirst()
                .MergeToUnit(model.DeferAction(_ => activated.action.DoTheExecute(model)).IgnoreElements()).TakeFirst();

        private static IObservable<(ActionBase action, IModelHotkeyAction model)> WhenActionActivated(this XafApplication application, (HotKeyManager manager, Frame window) hotKeyManager) 
            => hotKeyManager.window.Actions().Where(a => a.Available()).ToNowObservable().Select(@base => @base)
                .Concat(application.WhenFrameCreated().SelectMany(frame =>frame.Actions()))
                .WhenHotkey(application.Model)
                .SelectMany(t => t.action.WhenActivated().WhereAvailable().To(t).StartWith(t));
        

        private static IObservable<IModelHotkeyAction> WhenHotKeyPressed(this XafApplication application, HotKeyManager hotKeyManager) 
            => application.Model.ToReactiveModule<IModelReactiveModuleWindows>().Windows.HotkeyActions.ToNowObservable()
                .SelectMany(action => {
                    var modifiers = action.ParseShortcut();
                    if (action is IModelSystemAction) {
                        var globalHotKey = new GlobalHotKey(action.Id(), modifiers.modifier, modifiers.key);
                        hotKeyManager.AddGlobalHotKey(globalHotKey);
                        return hotKeyManager.WhenGlobalHotKeyPressed( globalHotKey).To(action);    
                    }
                    var localHotKey = new LocalHotKey(action.Id(), modifiers.modifier, modifiers.key);
                    hotKeyManager.AddLocalHotKey(localHotKey);
                    return hotKeyManager.WhenLocalHotKeyPressed( localHotKey).To( action);
                });

        private static IObservable<GlobalHotKeyEventArgs> WhenGlobalHotKeyPressed(this HotKeyManager hotKeyManager, GlobalHotKey globalHotKey) 
            => hotKeyManager.WhenEvent<GlobalHotKeyEventArgs>(nameof(HotKeyManager.GlobalHotKeyPressed)).Where(e => e.HotKey.Name==globalHotKey.Name);
        private static IObservable<LocalHotKeyEventArgs> WhenLocalHotKeyPressed(this HotKeyManager hotKeyManager, LocalHotKey globalHotKey) 
            => hotKeyManager.WhenEvent<LocalHotKeyEventArgs>(nameof(HotKeyManager.LocalHotKeyPressed)).Where(e => e.HotKey.Name==globalHotKey.Name);
        
        private static void DoTheExecute(this ActionBase action, IModelHotkeyAction model) {
            switch (action) {
                case SimpleAction simpleAction:
                    simpleAction.ExecuteIfAvailable();
                    break;
                case ParametrizedAction parametrizedAction:
                    parametrizedAction.ExecuteIfAvailable();
                    break;
                case SingleChoiceAction singleChoiceAction :
                    singleChoiceAction.ExecuteIfAvailable(singleChoiceAction.Items.First(item => (string)item.Data==model.ChoiceActionItem.Id));
                    break;
            }
        }

        private static IObservable<(HotKeyManager hotKeyManager, Frame window)> WhenHotKeyManager(this XafApplication application) 
            => application.WhenMainWindowCreated()
                .SelectMany(window => window.WhenTemplateChanged().StartWith(window).WhenNotDefault(frame => frame.Template)
                    .Select(_ => {
                        var hotKeyManager = new HotKeyManager((IWin32Window)window.Template);
                        CustomizeHotKeyManagerSubject.OnNext(hotKeyManager);
                        return (hotKeyManager, (Frame)window);
                    }));

        internal static bool IsModifier(this Keys keys) {
            switch (keys) {
                case Keys.Control:
                case Keys.ControlKey:
                case Keys.LControlKey:
                case Keys.RControlKey:
                case Keys.Shift:
                case Keys.ShiftKey:
                case Keys.LShiftKey:
                case Keys.RShiftKey:
                case Keys.Alt:
                case Keys.Menu:
                case Keys.LMenu:
                case Keys.RMenu:
                case Keys.LWin:
                    return true;
            }

            return false;
        }

        static (Modifiers modifier, Keys key) ParseShortcut(this IModelHotkeyAction model) {
            var hasAlt = false;
            var hasControl = false;
            var hasShift = false;
            var hasWin = false;
            var modifier = Modifiers.None;
            var current = 0;
            var separators = new[] { " + " };
            var result = model.HotKey.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            foreach (var entry in result) {
                if (entry.Trim() == Keys.Control.ToString()) {
                    hasControl = true;
                }
                if (entry.Trim() == Keys.Alt.ToString()) {
                    hasAlt = true;
                }
                if (entry.Trim() == Keys.Shift.ToString()) {
                    hasShift = true;
                }
                if (entry.Trim() == Keys.LWin.ToString() && current != result.Length - 1) {
                    hasWin = true;
                }
                current++;
            }
            if (hasControl) {
                modifier |= Modifiers.Control;
            }
            if (hasAlt) {
                modifier |= Modifiers.Alt;
            }
            if (hasShift) {
                modifier |= Modifiers.Shift;
            }
            if (hasWin) {
                modifier |= Modifiers.Win;
            }
            return  ( modifier, key:(Keys)new KeysConverter().ConvertFrom(result.GetValue(result.Length - 1))! );
        }

        private static IObservable<(ActionBase action, IModelHotkeyAction model)> WhenHotkey(this IObservable<ActionBase> source,IModelApplication application)
            => source.SelectMany(action => application.ToReactiveModule<IModelReactiveModuleWindows>().Windows.HotkeyActions
                .Where(hotkeyAction => hotkeyAction.Action!=null).Where(modelAction => action.Model.Id==modelAction.Action.Id())
                .Select(modelSystemAction => (action,modelSystemAction)));
    }

    
    
    
    
    

}