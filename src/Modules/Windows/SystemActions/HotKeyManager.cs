using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Windows.Forms;


namespace Xpand.XAF.Modules.Windows.SystemActions {
    public sealed class
        HotKeyManager : IMessageFilter,
            IDisposable // , IEnumerable, IEnumerable<GlobalHotKey>, IEnumerable<LocalHotKey>, IEnumerable<ChordHotKey>
    {
        #region **Properties and enum.

        /// <summary>Specifies the container to search in the HotKeyExists function.
        /// </summary>
        public enum CheckKey {
            /// <summary>Specifies that the HotKey should be checked against Local and Global HotKeys.
            /// </summary>
            Both = 0,

            /// <summary>Specifies that the HotKey should be checked against GlobalHotKeys only.
            /// </summary>
            GlobalHotKey = 1,

            /// <summary>Specifies that the HotKey should be checked against LocalHotKeys only.
            /// </summary>
            LocalHotKey = 2
        }

        readonly IntPtr _formHandle;
        private static readonly SerialCounter IDGen = new(-1); //Will keep track of all the registered GlobalHotKeys
        private IntPtr _hookId;
        private Win32.HookProc _callback;
        private bool _hooked;
        private bool _autoDispose;
        static bool _inChordMode; //Will determine if a chord has started.
        private Form ManagerForm => (Form)Control.FromHandle(_formHandle);

        private readonly List<GlobalHotKey> _globalHotKeyContainer = new(); //Will hold our GlobalHotKeys
        private readonly List<LocalHotKey> _localHotKeyContainer = new(); //Will hold our LocalHotKeys.
        private readonly List<ChordHotKey> _chordHotKeyContainer = new(); //Will hold our ChordHotKeys.

        //Keep the previous key and modifier that started a chord.
        Keys _preChordKey;
        Modifiers _preChordModifier;

        /// <summary>Determines if exceptions should be raised when an error occurs.
        /// </summary>
        public bool SuppressException { get; set; } //Determines if you want exceptions to be thrown.

        /// <summary>Gets or sets if the Manager should still function when its owner form is inactive.
        /// </summary>
        public bool DisableOnManagerFormInactive { get; set; }

        /// <summary>Determines if the HotKeyManager should be automatically disposed when the manager form is closed.
        /// </summary>
        [SuppressMessage("ReSharper", "EventUnsubscriptionViaAnonymousDelegate")]
        public bool AutoDispose {
            get => _autoDispose;
            set {
                if (value)
                    ManagerForm.FormClosed += delegate { Dispose(); };
                else
                    ManagerForm.FormClosing -= delegate { Dispose(); };

                _autoDispose = value;
            }
        }

        /// <summary>Determines if the manager is active.
        /// </summary>
        public bool Enabled { get; set; } //Refuse to listen to any windows message.

        /// <summary>Specifies if the keyboard has been hooked.
        /// </summary>
        public bool KeyboardHooked => _hooked;

        /// <summary>Returns the total number of registered GlobalHotkeys.
        /// </summary>
        public int GlobalHotKeyCount { get; private set; }

        /// <summary>Returns the total number of registered LocalHotkeys.
        /// </summary>
        public int LocalHotKeyCount { get; private set; }

        /// <summary>Returns the total number of registered ChordHotKeys.
        /// </summary>
        public int ChordHotKeyCount { get; private set; }

        /// <summary>Returns the total number of registered HotKey with the HotKeyManager.
        /// </summary>
        public int HotKeyCount => LocalHotKeyCount + GlobalHotKeyCount + ChordHotKeyCount;

        #endregion

        #region **Constructors.

        /// <summary>Creates a new HotKeyManager object.
        /// </summary>
        /// <param name="form">The form to associate hotkeys with. Must not be null.</param>
        /// <param name="suppressExceptions">Specifies if you want exceptions to be handled.</param>
        /// <exception cref="System.ArgumentNullException">thrown if the form is null.</exception>
        public HotKeyManager(IWin32Window form, bool suppressExceptions = false) {
            if (form == null)
                throw new ArgumentNullException(nameof(form));

            SuppressException = suppressExceptions;
            _formHandle = form.Handle;
            Enabled = true;
            AutoDispose = true;

            Application.AddMessageFilter(this); //Allow this class to receive Window messages.
        }

        #endregion

        #region **Event Handlers

        /// <summary>Will be raised if a registered GlobalHotKey is pressed
        /// </summary>
        public event GlobalHotKeyEventHandler GlobalHotKeyPressed;

        /// <summary>Will be raised if an local Hotkey is pressed.
        /// </summary>
        public event LocalHotKeyEventHandler LocalHotKeyPressed;

        /// <summary>Will be raised if a Key is help down on the keyboard.
        /// The keyboard has to be hooked for this event to be raised.
        /// </summary>
        public event KeyboardHookEventHandler KeyBoardKeyDown;

        /// <summary>Will be raised if a key is released on the keyboard.
        /// The keyboard has to be hooked for this event to be raised.
        /// </summary>
        public event KeyboardHookEventHandler KeyBoardKeyUp;

        /// <summary>Will be raised if a key is pressed on the keyboard.
        /// The keyboard has to be hooked for this event to be raised.
        /// </summary>
        public event KeyboardHookEventHandler KeyBoardKeyEvent;

        /// <summary>Will be raised if a key is pressed in the current application.
        /// </summary>
        public event HotKeyEventHandler KeyPressEvent;

        /// <summary>Will be raised if a Chord has started.
        /// </summary>
        public event PreChordHotkeyEventHandler ChordStarted;

        /// <summary>Will be raised if a chord is pressed.
        /// </summary>
        public event ChordHotKeyEventHandler ChordPressed;

        #endregion

        #region **Enumerations.

        /// <summary>Use for enumerating through all GlobalHotKeys.
        /// </summary>
        public IEnumerable EnumerateGlobalHotKeys => _globalHotKeyContainer;

        /// <summary>Use for enumerating through all LocalHotKeys.
        /// </summary>
        public IEnumerable EnumerateLocalHotKeys => _localHotKeyContainer;

        /// <summary>Use for enumerating through all ChordHotKeys.
        /// </summary>
        public IEnumerable EnumerateChordHotKeys => _chordHotKeyContainer;

        #endregion

        #region **Handle Property Changing.

        void GlobalHotKeyPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (sender is GlobalHotKey kvPair) {
                if (e.PropertyName == "Enabled") {
                    if (kvPair.Enabled)
                        RegisterGlobalHotKey(kvPair.Id, kvPair);
                    else
                        UnregisterGlobalHotKey(kvPair.Id);
                }
                else if (e.PropertyName == "Key" || e.PropertyName == "Modifier") {
                    if (kvPair.Enabled) {
                        UnregisterGlobalHotKey(_globalHotKeyContainer.IndexOf(kvPair));
                        RegisterGlobalHotKey(kvPair.Id, kvPair);
                    }
                }
            }
        }

        #endregion

        #region **Events, Methods and Helpers

        private void RegisterGlobalHotKey(int id, GlobalHotKey hotKey) {
            if ((int)_formHandle != 0) {
                if (hotKey.Key == Keys.LWin && (hotKey.Modifier & Modifiers.Win) == Modifiers.None)
                    Win32.RegisterHotKey(_formHandle, id, (int)(hotKey.Modifier | Modifiers.Win), (int)hotKey.Key);
                else
                    Win32.RegisterHotKey(_formHandle, id, (int)hotKey.Modifier, (int)(hotKey.Key));

                int error = Marshal.GetLastWin32Error();
                if (error != 0) {
                    if (!SuppressException) {
                        Exception e = new Win32Exception(error);

                        if (error == 1409)
                            throw new HotKeyAlreadyRegisteredException(e.Message, hotKey, e);
                        if (error != 2)
                            throw e; //ToDo: Fix here: File not found exception
                    }
                }
            }
            else if (!SuppressException) {
                throw new InvalidOperationException("Handle is invalid");
            }
        }

        private void UnregisterGlobalHotKey(int id) {
            if ((int)_formHandle != 0) {
                Win32.UnregisterHotKey(_formHandle, id);
                int error = Marshal.GetLastWin32Error();
                if (error != 0 && error != 2)
                    if (!SuppressException) {
                        throw new HotKeyUnregistrationFailedException("The hotkey could not be unregistered",
                            _globalHotKeyContainer[id], new Win32Exception(error));
                    }
            }
        }

        private class SerialCounter {
            public SerialCounter(int start) {
                Current = start;
            }

            private int Current { get; set; }

            public int Next() {
                return ++Current;
            }
        }

        /// <summary>Registers a GlobalHotKey if enabled.
        /// </summary>
        /// <param name="hotKey">The hotKey which will be added. Must not be null and can be registered only once.</param>
        /// <exception cref="HotKeyAlreadyRegisteredException">Thrown is a GlobalHotkey with the same name, and or key and modifier has already been added.</exception>
        /// <exception cref="System.ArgumentNullException">thrown if a the HotKey to be added is null, or the key is not specified.</exception>
        public bool AddGlobalHotKey(GlobalHotKey hotKey) {
            if (_globalHotKeyContainer.Contains(hotKey)) {
                if (!SuppressException)
                    throw new HotKeyAlreadyRegisteredException("HotKey already registered!", hotKey);
                return false;
            }
            int id = IDGen.Next();
            if (hotKey.Enabled)
                RegisterGlobalHotKey(id, hotKey);
            hotKey.Id = id;
            hotKey.PropertyChanged += GlobalHotKeyPropertyChanged;
            _globalHotKeyContainer.Add(hotKey);
            ++GlobalHotKeyCount;
            return true;
        }

        /// <summary>Registers a LocalHotKey.
        /// </summary>
        /// <param name="hotKey">The hotKey which will be added. Must not be null and can be registered only once.</param>
        /// <exception cref="HotKeyAlreadyRegisteredException">thrown if a LocalHotkey with the same name and or key and modifier has already been added.</exception>
        public bool AddLocalHotKey(LocalHotKey hotKey) {
            if (hotKey == null) {
                if (!SuppressException)
                    throw new ArgumentNullException(nameof(hotKey));

                return false;
            }

            if (hotKey.Key == 0) {
                if (!SuppressException)
                    throw new ArgumentNullException(nameof(hotKey));

                return false;
            }

            
            var chordExits = _chordHotKeyContainer.Exists(f => f.BaseKey == hotKey.Key && f.BaseModifier == hotKey.Modifier);
            if (_localHotKeyContainer.Contains(hotKey) || chordExits) {
                if (!SuppressException)
                    throw new HotKeyAlreadyRegisteredException("HotKey already registered!", hotKey);

                return false;
            }

            _localHotKeyContainer.Add(hotKey);
            ++LocalHotKeyCount;
            return true;
        }

        /// <summary>Registers a ChordHotKey.
        /// </summary>
        /// <param name="hotKey">The hotKey which will be added. Must not be null and can be registered only once.</param>
        /// <returns>True if registered successfully, false otherwise.</returns>
        /// <exception cref="HotKeyAlreadyRegisteredException">thrown if a LocalHotkey with the same name and or key and modifier has already been added.</exception>
        public bool AddChordHotKey(ChordHotKey hotKey) {
            if (hotKey == null) {
                if (!SuppressException)
                    throw new ArgumentNullException(nameof(hotKey));

                return false;
            }

            if (hotKey.BaseKey == 0 || hotKey.ChordKey == 0) {
                if (!SuppressException)
                    throw new ArgumentNullException(nameof(hotKey));

                return false;
            }

            //Check if a LocalHotKey already has its Key and Modifier.
            bool localExists = _localHotKeyContainer.Exists
            (
                f => (f.Key == hotKey.BaseKey && f.Modifier == hotKey.BaseModifier)
            );

            if (_chordHotKeyContainer.Contains(hotKey) || localExists) {
                if (!SuppressException)
                    throw new HotKeyAlreadyRegisteredException("HotKey already registered!", hotKey);

                return false;
            }

            _chordHotKeyContainer.Add(hotKey);
            ++ChordHotKeyCount;
            return true;
        }

        /// <summary>Unregisters a GlobalHotKey.
        /// </summary>
        /// <param name="hotKey">The hotKey to be removed</param>
        /// <returns>True if success, otherwise false</returns>
        public bool RemoveGlobalHotKey(GlobalHotKey hotKey) {
            if (_globalHotKeyContainer.Remove(hotKey)) {
                --GlobalHotKeyCount;

                if (hotKey.Enabled)
                    UnregisterGlobalHotKey(hotKey.Id);

                hotKey.PropertyChanged -= GlobalHotKeyPropertyChanged;
                return true;
            }

            return false;
        }

        /// <summary>Unregisters a LocalHotKey.
        /// </summary>
        /// <param name="hotKey">The hotKey to be removed</param>
        /// <returns>True if success, otherwise false</returns>
        public bool RemoveLocalHotKey(LocalHotKey hotKey) {
            if (_localHotKeyContainer.Remove(hotKey)) {
                --LocalHotKeyCount;
                return true;
            }

            return false;
        }

        /// <summary>Unregisters a ChordHotKey.
        /// </summary>
        /// <param name="hotKey">The hotKey to be removed</param>
        /// <returns>True if success, otherwise false</returns>
        public bool RemoveChordHotKey(ChordHotKey hotKey) {
            if (_chordHotKeyContainer.Remove(hotKey)) {
                --ChordHotKeyCount;
                return true;
            }

            return false;
        }

        /// <summary>Removes the hotkey(Local, Chord or Global) with the specified name.
        /// </summary>
        /// <param name="name">The name of the hotkey.</param>
        /// <returns>True if successful and false otherwise.</returns>
        public bool RemoveHotKey(string name) {
            LocalHotKey local = _localHotKeyContainer.Find
            (
                l => (l.Name == name)
            );

            if (local != null) {
                return RemoveLocalHotKey(local);
            }

            ChordHotKey chord = _chordHotKeyContainer.Find
            (
                c => (c.Name == name)
            );

            if (chord != null) {
                return RemoveChordHotKey(chord);
            }

            GlobalHotKey global = _globalHotKeyContainer.Find
            (
                g => (g.Name == name)
            );

            if (global != null) {
                return RemoveGlobalHotKey(global);
            }

            return false;
        }

        /// <summary>Checks if a HotKey has been registered.
        /// </summary>
        /// <param name="name">The name of the HotKey.</param>
        /// <returns>True if the HotKey has been registered, false otherwise.</returns>
        public bool HotKeyExists(string name) {
            LocalHotKey local = _localHotKeyContainer.Find
            (
                l => (l.Name == name)
            );

            if (local != null) {
                return true;
            }

            ChordHotKey chord = _chordHotKeyContainer.Find
            (
                c => (c.Name == name)
            );

            if (chord != null) {
                return true;
            }

            GlobalHotKey global = _globalHotKeyContainer.Find
            (
                g => (g.Name == name)
            );

            if (global != null) {
                return true;
            }

            return false;
        }

        /// <summary>Checks if a ChordHotKey has been registered.
        /// </summary>
        /// <param name="chordHotKey">The ChordHotKey to check.</param>
        /// <returns>True if the ChordHotKey has been registered, false otherwise.</returns>
        public bool HotKeyExists(ChordHotKey chordHotKey) {
            return _chordHotKeyContainer.Exists
            (
                c => (c.Equals(chordHotKey))
            );
        }

        /// <summary>Checks if a hotkey has already been registered as a Local or Global HotKey.
        /// </summary>
        /// <param name="shortcut">The hotkey string to check.</param>
        /// <param name="toCheck">The HotKey type to check.</param>
        /// <returns>True if the HotKey is already registered, false otherwise.</returns>
        public bool HotKeyExists(string shortcut, CheckKey toCheck) {
            throw new NotImplementedException();
            // Keys key = (Keys)HotKeyShared.ParseShortcut(shortcut).GetValue(1)!;
            // Modifiers modifier = (Modifiers)HotKeyShared.ParseShortcut(shortcut).GetValue(0)!;
            // switch (toCheck) {
            //     case CheckKey.GlobalHotKey:
            //         return _globalHotKeyContainer.Exists
            //         (
            //             g => (g.Key == key && g.Modifier == modifier)
            //         );
            //
            //     case CheckKey.LocalHotKey:
            //         return (_localHotKeyContainer.Exists
            //                 (
            //                     l => (l.Key == key && l.Modifier == modifier)
            //                 )
            //                 |
            //                 _chordHotKeyContainer.Exists
            //                 (
            //                     c => (c.BaseKey == key && c.BaseModifier == modifier)));
            //
            //     case CheckKey.Both:
            //         return (HotKeyExists(shortcut, CheckKey.GlobalHotKey) ^
            //                 HotKeyExists(shortcut, CheckKey.LocalHotKey));
            // }
            //
            // return false;
        }

        /// <summary>Checks if a hotkey has already been registered as a Local or Global HotKey.
        /// </summary>
        /// <param name="key">The key of the HotKey.</param>
        /// <param name="modifier">The modifier of the HotKey.</param>
        /// <param name="toCheck">The HotKey type to check.</param>
        /// <returns>True if the HotKey is already registered, false otherwise.</returns>
        public bool HotKeyExists(Keys key, Modifiers modifier, CheckKey toCheck) {
            return (HotKeyExists(HotKeyShared.CombineShortcut(modifier, key), toCheck));
        }

        #endregion

        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
        public bool PreFilterMessage(ref Message m) {
            if (!Enabled) {
                return false;
            }

            //Check if the form that the HotKeyManager is registered to is inactive.
            if (DisableOnManagerFormInactive)
                if (Form.ActiveForm != null && ManagerForm != Form.ActiveForm) {
                    return false;
                }

            //For LocalHotKeys, determine if modifiers Alt, Shift and Control is pressed.
            var userKeyBoard = new Microsoft.VisualBasic.Devices.Keyboard();
            var altPressed = userKeyBoard.AltKeyDown;
            var controlPressed = userKeyBoard.CtrlKeyDown;
            var shiftPressed = userKeyBoard.ShiftKeyDown;

            var localModifier = Modifiers.None;
            if (altPressed) {
                localModifier = Modifiers.Alt;
            }

            if (controlPressed) {
                localModifier |= Modifiers.Control;
            }

            if (shiftPressed) {
                localModifier |= Modifiers.Shift;
            }

            switch ((KeyboardMessages)m.Msg) {
                case (KeyboardMessages.WmSyskeydown):
                case (KeyboardMessages.WmKeydown):
                    var keydownCode = (Keys)(int)m.WParam & Keys.KeyCode;
                    if (KeyPressEvent != null)
                        KeyPressEvent(this, new HotKeyEventArgs(keydownCode, localModifier, RaiseLocalEvent.OnKeyDown));
                    if (_inChordMode) {
                        if (keydownCode.IsModifier()) return true;

                        var chordMain = _chordHotKeyContainer.Find(
                            cm => cm.BaseKey == _preChordKey && cm.BaseModifier == _preChordModifier &&
                                  cm.ChordKey == keydownCode && cm.ChordModifier == localModifier
                        );

                        if (chordMain != null) {
                            chordMain.RaiseOnHotKeyPressed();
                            if (ChordPressed != null && chordMain.Enabled)
                                ChordPressed(this, new ChordHotKeyEventArgs(chordMain));
                            _inChordMode = false;
                            return true;
                        }

                        _inChordMode = false;
                        new Microsoft.VisualBasic.Devices.Computer().Audio.PlaySystemSound(System.Media.SystemSounds.Exclamation);
                        return true;
                    }
                    var keyDownHotkey = _localHotKeyContainer.Find(d => d.Key == keydownCode && d.Modifier == localModifier && d.WhenToRaise == RaiseLocalEvent.OnKeyDown);
                    if (keyDownHotkey != null) {
                        keyDownHotkey.RaiseOnHotKeyPressed();
                        if (LocalHotKeyPressed != null && keyDownHotkey.Enabled)
                            LocalHotKeyPressed(this, new LocalHotKeyEventArgs(keyDownHotkey));

                        return keyDownHotkey.SuppressKeyPress;
                    }
                    
                    var chordBase = _chordHotKeyContainer.Find(c => c.BaseKey == keydownCode && c.BaseModifier == localModifier);
                    if (chordBase != null) {
                        _preChordKey = chordBase.BaseKey;
                        _preChordModifier = chordBase.BaseModifier;
                        var e = new PreChordHotKeyEventArgs(new LocalHotKey(chordBase.Name, chordBase.BaseModifier,
                            chordBase.BaseKey));
                        if (ChordStarted != null)
                            ChordStarted(this, e);

                        _inChordMode = !e.HandleChord;
                        return true;
                    }

                    _inChordMode = false;
                    return false;
                case KeyboardMessages.WmSyskeyup:
                case KeyboardMessages.WmKeyup:
                    var keyupCode = (Keys)(int)m.WParam & Keys.KeyCode;
                    if (KeyPressEvent != null)
                        KeyPressEvent(this, new HotKeyEventArgs(keyupCode, localModifier, RaiseLocalEvent.OnKeyDown));
                    var keyUpHotkey = _localHotKeyContainer.Find(
                        u => u.Key == keyupCode && u.Modifier == localModifier && u.WhenToRaise == RaiseLocalEvent.OnKeyUp
                    );
                    if (keyUpHotkey != null) {
                        keyUpHotkey.RaiseOnHotKeyPressed();
                        if (LocalHotKeyPressed != null && keyUpHotkey.Enabled)
                            LocalHotKeyPressed(this, new LocalHotKeyEventArgs(keyUpHotkey));
                        return keyUpHotkey.SuppressKeyPress;
                    }

                    return false;

                case KeyboardMessages.WmHotKey:
                    var id = (int)m.WParam;
                    var pressed = _globalHotKeyContainer.Find(
                        g => g.Id == id
                    );
                    pressed?.RaiseOnHotKeyPressed();
                    if (GlobalHotKeyPressed != null)
                        GlobalHotKeyPressed(this, new GlobalHotKeyEventArgs(pressed));
                    return true;

                default: return false;
            }
        }

        
        private void OnKeyboardKeyDown(KeyboardHookEventArgs e) {
            if (KeyBoardKeyDown != null)
                KeyBoardKeyDown(this, e);
            OnKeyboardKeyEvent(e);
        }

        private void OnKeyboardKeyUp(KeyboardHookEventArgs e) {
            if (KeyBoardKeyUp != null)
                KeyBoardKeyUp(this, e);
            OnKeyboardKeyEvent(e);
        }

        private void OnKeyboardKeyEvent(KeyboardHookEventArgs e) {
            if (KeyBoardKeyEvent != null)
                KeyBoardKeyEvent(this, e);
        }

        /// <summary>Allows the application to listen to all keyboard messages.
        /// </summary>
        public void KeyBoardHook() {
            _callback = KeyboardHookCallback;
            _hookId = Win32.SetWindowsHook((int)KeyboardHookEnum.KeyboardHook, _callback);
            _hooked = true;
        }

        /// <summary>Stops the application from listening to all keyboard messages.
        /// </summary>
        public void KeyBoardUnHook() {
            try {
                if (!_hooked) return;
                Win32.UnhookWindowsHookEx(_hookId);
                _callback = null;
                _hooked = false;
            }
            catch (MarshalDirectiveException) {
                //if (!SuppressException) throw (e);
            }
        }

        /// <summary>
        /// This is the call-back method that is called whenever a keyboard event is triggered.
        /// We use it to call our individual custom events.
        /// </summary>
        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam) {
            if (!Enabled) return Win32.CallNextHookEx(_hookId, nCode, wParam, lParam);

            if (nCode >= 0) {
                var lParamStruct = (KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct))!;
                var e = new KeyboardHookEventArgs(lParamStruct);
                switch ((KeyboardMessages)wParam) {
                    case KeyboardMessages.WmSyskeydown:
                    case KeyboardMessages.WmKeydown:
                        e.KeyboardEventName = KeyboardEventNames.KeyDown;
                        OnKeyboardKeyDown(e);
                        break;

                    case KeyboardMessages.WmSyskeyup:
                    case KeyboardMessages.WmKeyup:
                        e.KeyboardEventName = KeyboardEventNames.KeyUp;
                        OnKeyboardKeyUp(e);
                        break;
                }

                if (e.Handled) {
                    return (IntPtr)(-1);
                }
            }

            return Win32.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }



        #region **Simulation.

        /// <summary>Simulates pressing a key.
        /// </summary>
        /// <param name="key">The key to press.</param>
        public void SimulateKeyDown(Keys key) {
            Win32.keybd_event(ParseKey(key), 0, 0, 0);
        }

        public void SimulateKeyUp(Keys key) {
            Win32.keybd_event(ParseKey(key), 0, (int)KeyboardHookEnum.KeyboardKeyUp, 0);
        }

        /// <summary>Simulates pressing a key. The key is pressed, then released.
        /// </summary>
        /// <param name="key">The key to press.</param>
        public void SimulateKeyPress(Keys key) {
            SimulateKeyDown(key);
            SimulateKeyUp(key);
        }

        static byte ParseKey(Keys key) {
            // Alt, Shift, and Control need to be changed for API function to work with them
            switch (key) {
                case Keys.Alt:
                    return 18;
                case Keys.Control:
                    return 17;
                case Keys.Shift:
                    return 16;
                default:
                    return (byte)key;
            }
        }

        #endregion

        #region **Destructor

        private bool _disposed;

        private void Dispose(bool disposing) {
            if (_disposed)
                return;

            if (disposing) {
                Application.RemoveMessageFilter(this);
                SuppressException = true;
            }

            for (int i = _globalHotKeyContainer.Count - 1; i >= 0; i--) {
                RemoveGlobalHotKey(_globalHotKeyContainer[i]);
            }

            _localHotKeyContainer.Clear();
            _chordHotKeyContainer.Clear();
            KeyBoardUnHook();
            _disposed = true;
        }

        /// <summary>Destroys and releases all memory used by this class.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~HotKeyManager() {
            Dispose(false);
        }

        #endregion
    }

    #region **Exceptions

    [Serializable]
    public class HotKeyAlreadyRegisteredException : Exception {
        public GlobalHotKey HotKey { get; private set; }
        public LocalHotKey LocalKey { get; private set; }
        public ChordHotKey ChordKey { get; private set; }

        public HotKeyAlreadyRegisteredException(string message, GlobalHotKey hotKey) : base(message) {
            HotKey = hotKey;
        }

        public HotKeyAlreadyRegisteredException(string message, GlobalHotKey hotKey, Exception inner) : base(message,
            inner) {
            HotKey = hotKey;
        }

        public HotKeyAlreadyRegisteredException(string message, LocalHotKey hotKey) : base(message) {
            LocalKey = hotKey;
        }

        public HotKeyAlreadyRegisteredException(string message, LocalHotKey hotKey, Exception inner) : base(message,
            inner) {
            LocalKey = hotKey;
        }

        public HotKeyAlreadyRegisteredException(string message, ChordHotKey hotKey) : base(message) {
            ChordKey = hotKey;
        }

        public HotKeyAlreadyRegisteredException(string message, ChordHotKey hotKey, Exception inner) : base(message,
            inner) {
            ChordKey = hotKey;
        }

        
    }

    [Serializable]
    public class HotKeyUnregistrationFailedException : Exception {
        public GlobalHotKey HotKey { get; private set; }

        public HotKeyUnregistrationFailedException(string message, GlobalHotKey hotKey) : base(message) {
            HotKey = hotKey;
        }

        public HotKeyUnregistrationFailedException(string message, GlobalHotKey hotKey, Exception inner) : base(message,
            inner) {
            HotKey = hotKey;
        }

        
    }

    [Serializable]
    public class HotKeyRegistrationFailedException : Exception {
        public GlobalHotKey HotKey { get; private set; }

        public HotKeyRegistrationFailedException(string message, GlobalHotKey hotKey) : base(message) {
            HotKey = hotKey;
        }

        public HotKeyRegistrationFailedException(string message, GlobalHotKey hotKey, Exception inner) : base(message,
            inner) {
            HotKey = hotKey;
        }

        
    }

    [Serializable]
    public class HotKeyInvalidNameException : Exception {
        public HotKeyInvalidNameException(string message) : base(message) { }
        public HotKeyInvalidNameException(string message, Exception inner) : base(message, inner) { }

        
    }

    #endregion
}