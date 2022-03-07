using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Win;
using JetBrains.Annotations;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.Windows {
    

    public static class KeyboardService {
        [SuppressMessage("ReSharper", "InconsistentNaming")] 
        private const int WH_KEYBOARD_LL = 13;
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private const int WM_KEYDOWN = 0x0100;
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private const int WM_KEYUP = 0x0101;
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private const int WM_SYSKEYDOWN = 0x0104;
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private const int WM_SYSKEYUP = 0x0105;
        private static readonly IntPtr HookId;
        [SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable")] [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static readonly LowLevelKeyboardProc _lowLevelKeyboardProc;
        private static readonly Dictionary<KeyboardStruct, Action> RegisteredCallbacks=new();
        private static readonly HashSet<ModifierKeys> DownModifierKeys=new();
        private static readonly HashSet<Keys> DownKeys=new();
        private static readonly object ModifiersLock = new();
        
        static KeyboardService() {
            _lowLevelKeyboardProc = HookCallback;
            HookId = SetHook(_lowLevelKeyboardProc);
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc) {
            var intPtr = LoadLibrary("User32");
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, intPtr, 0);
        }

        internal static IObservable<Unit> SystemActionsConnect(this XafApplication application) 
            => application.WhenFrameCreated().SelectMany(frame => frame.Actions().ToNowObservable().ExecuteHotkey()
                    .Merge(frame.Actions().ToNowObservable().UnregisterHotkey()))
                .ToUnit();
        
        private static IObservable<ActionBase> UnregisterHotkey(this IObservable<ActionBase> source)
            => source.WhenDeactivated().WhenSystem().Do(a => a.Keys().UnregisterHotkey());
        
        private static IObservable<ActionBase> ExecuteHotkey(this IObservable<ActionBase> source) 
            => source.WhenActivated().WhenSystem()
                .Do(a => a.Keys().RegisterHotkey(() => a.DoTheExecute()));

        public static Keys Keys(this ActionBase a) => ShortcutHelper.ParseBarShortcut(a.Shortcut).Key;

        private static IObservable<ActionBase> WhenSystem(this IObservable<ActionBase> source)
            => source.SelectMany(a => a.Application.ToReactiveModule<IModelReactiveModuleWindows>()
                .SelectMany(windows => windows.Windows.SystemActions.Select(link => link.Action).WhereNotDefault()
                    .Where(modelAction => !string.IsNullOrEmpty(modelAction.Shortcut)).Where(modelAction => a.Model==modelAction)
                    .To(a)));
        
        [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Local")]
        static Guid RegisterHotkey(this Keys virtualKeyCode, Action action) 
            => Array.Empty<ModifierKeys>().RegisterHotkey( virtualKeyCode, action);
        
        [UsedImplicitly]
        static Guid RegisterHotkey(this ModifierKeys modifiers, Keys virtualKeyCode, Action action) 
            => Enum.GetValues(typeof(ModifierKeys)).Cast<ModifierKeys>().ToArray()
                               .Where(modifier => modifiers.HasFlag(modifier)).ToArray()
                               .RegisterHotkey( virtualKeyCode, action);

        static Guid RegisterHotkey(this ModifierKeys[] modifiers, Keys virtualKeyCode, Action action) {
            var keybindIdentity = Guid.NewGuid();
            var keybind = new KeyboardStruct(modifiers, virtualKeyCode, keybindIdentity);
            if (RegisteredCallbacks.ContainsKey(keybind)) {
                throw new HotkeyAlreadyRegisteredException();
            }
            RegisteredCallbacks[keybind] = action;
            return keybindIdentity;
        }
        
        [UsedImplicitly]
        static void UnregisterAll() => RegisteredCallbacks.Clear();
        
        static void UnregisterHotkey(this Keys virtualKeyCode) => Array.Empty<ModifierKeys>().UnregisterHotkey( virtualKeyCode);

        static void UnregisterHotkey(this ModifierKeys[] modifiers, Keys virtualKeyCode) {
            if (!RegisteredCallbacks.Remove(new KeyboardStruct(modifiers, virtualKeyCode))) {
                throw new HotkeyNotRegisteredException();
            }
        }
        
        [UsedImplicitly]
        static void UnregisterHotkey(Guid identity) {
            var keybindToRemove = RegisteredCallbacks.Keys.FirstOrDefault(keyboard =>
                keyboard.Identifier.HasValue && keyboard.Identifier.Value.Equals(identity));

            if (keybindToRemove == null || !RegisteredCallbacks.Remove(keybindToRemove)) {
                throw new HotkeyNotRegisteredException();
            }
        }

        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
        private static void HandleKeyPress(this Keys virtualKeyCode) {
            var currentKey = new KeyboardStruct(DownModifierKeys, virtualKeyCode);

            if (!RegisteredCallbacks.ContainsKey(currentKey)) {
                return;
            }

            if (RegisteredCallbacks.TryGetValue(currentKey, out var callback)) {
                callback.Invoke();
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam) {
            if (nCode >= 0) {
                ThreadPool.QueueUserWorkItem(HandleSingleKeyboardInput, new KeyboardParams(wParam, Marshal.ReadInt32(lParam)));
            }
            return CallNextHookEx(HookId, nCode, wParam, lParam);
        }
        
        private static void HandleSingleKeyboardInput(object keyboardParamsObj) {
            var keyboardParams = (KeyboardParams)keyboardParamsObj;
            var wParam = keyboardParams.WParam;
            var vkCode = (Keys)keyboardParams.VkCode;
            var modifierKey = ModifierKeysUtilities.GetModifierKeyFromCode((int)vkCode);
            if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN) {
                if (modifierKey != null) {
                    lock (ModifiersLock) {
                        DownModifierKeys.Add(modifierKey.Value);
                    }
                }
                if (!DownKeys.Contains(vkCode)) {
                    HandleKeyPress(vkCode);
                    DownKeys.Add(vkCode);
                }
            }
            if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP) {
                if (modifierKey != null) {
                    lock (ModifiersLock) {
                        DownModifierKeys.Remove(modifierKey.Value);
                    }
                }
                DownKeys.Remove(vkCode);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        [UsedImplicitly]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        
        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string lpFileName);

    }

    internal class KeyboardHookException : Exception { }
    internal class HotkeyAlreadyRegisteredException : KeyboardHookException { }
    internal class HotkeyNotRegisteredException : KeyboardHookException { }
    
    internal class KeyboardStruct : IEquatable<KeyboardStruct> {
        public readonly Keys VirtualKeyCode;
        public readonly List<ModifierKeys> Modifiers;
        public readonly Guid? Identifier;

        public KeyboardStruct(IEnumerable<ModifierKeys> modifiers, Keys virtualKeyCode, Guid? identifier = null) {
            VirtualKeyCode = virtualKeyCode;
            Modifiers = new List<ModifierKeys>(modifiers);
            Identifier = identifier;
        }

        public bool Equals(KeyboardStruct other) 
            => other != null && (VirtualKeyCode == other.VirtualKeyCode &&
                                 (Modifiers.Count == other.Modifiers.Count 
                                  && Modifiers.All(modifier => other.Modifiers.Contains(modifier))));

        public override bool Equals(object obj) 
            => !ReferenceEquals(null, obj) 
               && (ReferenceEquals(this, obj) || obj.GetType() == GetType() && Equals((KeyboardStruct)obj));

        public override int GetHashCode() {
            var hash = 13;
            hash = (hash * 7) + VirtualKeyCode.GetHashCode();
            var modifiersHashSum = Modifiers.Sum(modifier => modifier.GetHashCode());
            hash = (hash * 7) + modifiersHashSum;
            return hash;
        }
    }
    
    [Flags]
    public enum ModifierKeys {
        Alt = 1,
        Control = 2,
        Shift = 4,
        WindowsKey = 8,
    }

    public static class ModifierKeysUtilities {
        public static ModifierKeys? GetModifierKeyFromCode(int keyCode) {
            switch (keyCode) {
                case 0xA0:
                case 0xA1:
                case 0x10:
                    return ModifierKeys.Shift;
                case 0xA2:
                case 0xA3:
                case 0x11:
                    return ModifierKeys.Control;
                case 0x12:
                case 0xA4:
                case 0xA5:
                    return ModifierKeys.Alt;
                case 0x5B:
                case 0x5C:
                    return ModifierKeys.WindowsKey;
                default:
                    return null;
            }
        }
    }
    struct KeyboardParams {
        public IntPtr WParam;
        public readonly int VkCode;

        public KeyboardParams(IntPtr wParam, int vkCode) {
            WParam = wParam;
            VkCode = vkCode;
        }
    }
}