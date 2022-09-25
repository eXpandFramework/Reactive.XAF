using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Reactive.Services.Controllers;

namespace Xpand.XAF.Modules.Windows.SystemActions {
    public static class SystemActionsService {
        private static readonly Subject<HotKeyManager> CustomizeHotKeyManagerSubject = new();
        public static IObservable<HotKeyManager> CustomizeHotKeyManager => CustomizeHotKeyManagerSubject.AsObservable();

        internal static IObservable<Unit> SystemActionsConnect(this XafApplication application) {
            var hotKeyManager = application.WhenHotKeyManager().Publish().RefCount();
            return hotKeyManager.ExecuteViewAgnosticHotKeys(application)
                // .Merge(hotKeyManager.ExecuteViewHotKeys(application))
                ;
        }

        private static IObservable<Unit> ExecuteViewHotKeys(this IObservable<HotKeyManager> source,XafApplication application)
            => source.ExecuteHotKey(application.WhenFrameViewChanged(),action => action.Views.Any())
                .SelectMany(t => t.action.View().WhenClosed().Do(_ => t.manager.RemoveHotKey(t.globalHotKey.Name)))
                .ToUnit();

        private static IObservable<Unit> ExecuteViewAgnosticHotKeys(
            this IObservable<(HotKeyManager manager, Frame window)> source, XafApplication application)
        
            => source.SelectMany(hotKeyManager => application.WhenGlobalHotKeyPressed(hotKeyManager.manager).Publish(hotKeyPressed => hotKeyPressed
                        .WithLatestFrom(application.WhenActionActivated(hotKeyManager), (pressed,activated) => (activated, pressed))
                        .Where(t => t.activated.modelSystemAction.Action == t.pressed.action.Action)))
                .DoWhen(t => t.pressed.action.Focus,_ => SetForegroundWindow(((Form)application.MainWindow.Template).Handle))
                .SelectAndOmit(t => t.activated.action.WhenExecuteCompleted().FirstAsync().Select(a => a)
                    .MergeToUnit(t.Defer(() => {
                        t.activated.action.DoTheExecute(t.pressed.action);
                        return Observable.Empty<Unit>();
                    })).FirstAsync())
                .ToUnit();

        private static IObservable<(ActionBase action, IModelSystemAction modelSystemAction)> WhenActionActivated(this XafApplication application, (HotKeyManager manager, Frame window) hotKeyManager) 
            => hotKeyManager.window.Actions().Where(a => a.Available()).ToNowObservable()
                .Concat(application.WhenWindowCreated().SelectMany(frame => frame.Actions()))
                .SelectMany(a => a.Controller.WhenActivated().To(a).StartWith(a).WhenAvailable()).WhenSystem()
                .Select(t => t);

        private static IObservable<(GlobalHotKeyEventArgs e, IModelSystemAction action)> WhenGlobalHotKeyPressed(this XafApplication application, HotKeyManager hotKeyManager) 
            => application.Model.ToReactiveModule<IModelReactiveModuleWindows>().Windows.SystemActions.ToNowObservable()
                .SelectMany(action => {
                    var modifiers = action.ParseShortcut();
                    var globalHotKey = new GlobalHotKey(action.Id(), modifiers.modifier, modifiers.key);
                    hotKeyManager.AddGlobalHotKey(globalHotKey);
                    return hotKeyManager.WhenGlobalHotKeyPressed( globalHotKey).Select(e => (e,action));
                });

        private static IObservable<GlobalHotKeyEventArgs> WhenGlobalHotKeyPressed(this HotKeyManager hotKeyManager, GlobalHotKey globalHotKey) 
            => hotKeyManager.WhenEvent<GlobalHotKeyEventArgs>(nameof(HotKeyManager.GlobalHotKeyPressed)).Where(e => e.HotKey.Name==globalHotKey.Name);

        private static IObservable<(ActionBase action, HotKeyManager manager, GlobalHotKey globalHotKey, IModelSystemAction modelSystemAction)> ExecuteHotKey(this IObservable<HotKeyManager> source, 
            IObservable<Frame> frameSource, Func<IModelSystemAction, bool> matchModel) 
            => source.CombineLatest(frameSource.SelectMany(frame => frame.Actions()).WhenSystem(),
                    (manager, t) => (manager, t.action, t.modelSystemAction)).Where(t => matchModel(t.modelSystemAction))
                .Select(t => (t.manager, t.action,shortcut:t.modelSystemAction.ParseShortcut(),t.modelSystemAction))
                .AddGlobalHotKey().Execute();

        private static IObservable<(ActionBase action, HotKeyManager manager, GlobalHotKey globalHotKey, IModelSystemAction modelSystemAction)> AddGlobalHotKey(
            this IObservable<(HotKeyManager manager, ActionBase action, (Modifiers modifier, Keys key) shortcut,IModelSystemAction modelSystemAction)> source)
            => source.Select(t => {
                var hotKey = t.manager.EnumerateGlobalHotKeys.Cast<GlobalHotKey>().FirstOrDefault(key => key.Name==t.action.Id);
                if (hotKey==null) {
                        var globalHotKey = new GlobalHotKey(t.action.Id, t.shortcut.modifier, t.shortcut.key);
                        t.manager.AddGlobalHotKey(globalHotKey);
                        return (t.action,t.manager,globalHotKey,t.modelSystemAction);
                }
                return (t.action,t.manager,hotKey,t.modelSystemAction);
            });

        private static IObservable<(ActionBase action, HotKeyManager manager, GlobalHotKey globalHotKey, IModelSystemAction modelSystemAction)> 
            Execute(this IObservable<(ActionBase action, HotKeyManager manager, GlobalHotKey globalHotKey, IModelSystemAction modelSystemAction)> source)
            => source.MergeIgnored(t => t.manager.WhenEvent<GlobalHotKeyEventArgs>(nameof(HotKeyManager.GlobalHotKeyPressed)).Where(e => e.HotKey.Name==t.globalHotKey.Name)
                .ObserveOnContext().Do(_ => t.action.DoTheExecute(t.modelSystemAction)));

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);
        
        private static void DoTheExecute(this ActionBase action, IModelSystemAction model) {
            switch (action) {
                case SimpleAction simpleAction:
                    simpleAction.DoExecute();
                    break;
                case SingleChoiceAction singleChoiceAction :
                    singleChoiceAction.ExecuteIfAvailable(singleChoiceAction.Items.First(item => (string)item.Data==model.ChoiceActionItem.Id));
                    break;
            }
        }

        private static IObservable<(HotKeyManager hotKeyManager, Frame window)> WhenHotKeyManager(this XafApplication application) 
            => application.WhenFrameCreated(TemplateContext.ApplicationWindow).TemplateChanged()
                .Select(window => {
                    var hotKeyManager = new HotKeyManager((IWin32Window)window.Template);
                    CustomizeHotKeyManagerSubject.OnNext(hotKeyManager);
                    return (hotKeyManager,window);
                });

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

        static (Modifiers modifier, Keys key) ParseShortcut(this IModelSystemAction model) {
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

        private static IObservable<(ActionBase action, IModelSystemAction modelSystemAction)> WhenSystem(this IObservable<ActionBase> source)
            => source.SelectMany(action => action.Application.Model.ToReactiveModule<IModelReactiveModuleWindows>().Windows.SystemActions
                .Where(modelSystemAction => modelSystemAction.Action!=null)
                    .Where(modelAction => action.Model.Id==modelAction.Action.Id())
                .Select(modelSystemAction => (action,modelSystemAction)));
    }

    
    
    
    
    

}