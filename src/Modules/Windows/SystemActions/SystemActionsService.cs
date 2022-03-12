using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Windows.SystemActions {
    public static class SystemActionsService {
        private static readonly Subject<HotKeyManager> CustomizeHotKeyManagerSubject = new();
        public static IObservable<HotKeyManager> CustomizeHotKeyManager => CustomizeHotKeyManagerSubject.AsObservable();

        internal static IObservable<Unit> SystemActionsConnect(this XafApplication application) {
            var hotKeyManager = application.WhenHotKeyManager().Publish().RefCount();
            return hotKeyManager.ExecuteViewAgnosticHotKeys(application)
                .Merge(hotKeyManager.ExecuteViewHotKeys(application));
        }

        private static IObservable<Unit> ExecuteViewHotKeys(this IObservable<HotKeyManager> source,XafApplication application)
            => source.ExecuteHotKey(application.WhenFrameViewChanged(),action => action.Views.Any())
                .SelectMany(t => t.action.View().WhenClosed().Do(_ => t.manager.RemoveHotKey(t.globalHotKey.Name)))
                .ToUnit();
        
        private static IObservable<Unit> ExecuteViewAgnosticHotKeys(this IObservable<HotKeyManager> source,XafApplication application) 
            => source.ExecuteHotKey(application.WhenFrameCreated(),action => !action.Views.Any()).ToUnit();

        private static IObservable<(ActionBase action, HotKeyManager manager, GlobalHotKey globalHotKey)> ExecuteHotKey(this IObservable<HotKeyManager> source, 
            IObservable<Frame> frameSource, Func<IModelSystemAction, bool> matchModel) 
            => source.CombineLatest(frameSource.SelectMany(frame => frame.Actions()).WhenSystem(),
                    (manager, t) => (manager, t.action, t.modelSystemAction)).Where(t => matchModel(t.modelSystemAction))
                .Select(t => (t.manager, t.action,shortcut:t.modelSystemAction.ParseShortcut()))
                .AddGlobalHotKey().Execute();

        private static IObservable<(ActionBase action, HotKeyManager manager, GlobalHotKey globalHotKey)> AddGlobalHotKey(
            this IObservable<(HotKeyManager manager, ActionBase action, (Modifiers modifier, Keys key) shortcut)> source)
            => source.Select(t => {
                    var globalHotKey = new GlobalHotKey(t.action.Id, t.shortcut.modifier, t.shortcut.key);
                    if (!t.manager.HotKeyExists(globalHotKey.Name)) {
                        t.manager.AddGlobalHotKey(globalHotKey);   
                    }
                    return (t.action,t.manager,globalHotKey);
                });
        
        private static IObservable<(ActionBase action, HotKeyManager manager, GlobalHotKey globalHotKey)> Execute(this IObservable<(ActionBase action, HotKeyManager manager, GlobalHotKey globalHotKey)> source)
            => source.MergeIgnored(t => t.manager.WhenEvent<GlobalHotKeyEventArgs>(nameof(HotKeyManager.GlobalHotKeyPressed)).Where(e => e.HotKey.Name==t.globalHotKey.Name)
                    .Do(_ => t.action.DoTheExecute()));
        
        private static IObservable<HotKeyManager> WhenHotKeyManager(this XafApplication application) 
            => application.WhenFrameCreated(TemplateContext.ApplicationWindow).TemplateChanged()
                .Select(window => {
                    var hotKeyManager = new HotKeyManager((IWin32Window)window.Template);
                    CustomizeHotKeyManagerSubject.OnNext(hotKeyManager);
                    return hotKeyManager;
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
                    .Where(modelAction => action.Model.Id==modelAction.Id())
                .Select(modelSystemAction => (action,modelSystemAction)));
    }

    
    
    
    
    

}