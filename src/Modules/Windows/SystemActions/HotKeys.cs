using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Windows.Forms;


namespace Xpand.XAF.Modules.Windows.SystemActions {
    #region **GlobalHotKey Class

    [Serializable]
    public class GlobalHotKey : INotifyPropertyChanged, ISerializable, IEquatable<GlobalHotKey> {
        #region **Properties

        private string _name;
        private Keys _key;
        private Modifiers _modifier;
        private bool _enabled;
        private object _tag;

        public int Id { get; internal set; }

        public string Name {
            get => _name;
            private set => _name = value;
        }

        public Keys Key {
            get => _key;
            set {
                if (_key != value) {
                    _key = value;
                    OnPropertyChanged("Key");
                }
            }
        }

        public Modifiers Modifier {
            get => _modifier;
            set {
                if (_modifier != value) {
                    _modifier = value;
                    OnPropertyChanged("Modifier");
                }
            }
        }

        public bool Enabled {
            get => _enabled;
            set {
                if (value != _enabled) {
                    _enabled = value;
                    OnPropertyChanged("Enabled");
                }
            }
        }

        public object Tag {
            get => _tag;
            set => _tag = value;
        }

        #endregion

        #region **Event Handlers

        public event PropertyChangedEventHandler PropertyChanged;

        public event GlobalHotKeyEventHandler HotKeyPressed;

        #endregion

        #region **Constructor

        public GlobalHotKey(string name, Modifiers modifier, Keys key, bool enabled = true) {
            Name = name;
            Key = key;
            Modifier = modifier;
            Enabled = enabled;
        }

        public GlobalHotKey(string name, Modifiers modifier, int key, bool enabled = true) {
            Name = name;
            Key = (Keys)key;
            Modifier = modifier;
            Enabled = enabled;
        }

        protected GlobalHotKey(SerializationInfo info, StreamingContext context) {
            Name = info.GetString("Name");
            Key = (Keys)info.GetValue("Key", typeof(Keys))!;
            Modifier = (Modifiers)info.GetValue("Modifiers", typeof(Modifiers))!;
            Enabled = info.GetBoolean("Enabled");
        }

        #endregion

        #region **Events, Methods and Helpers

        public bool Equals(GlobalHotKey other) =>
            Key == other!.Key && Modifier == other.Modifier || Name.ToLower() == other.Name.ToLower();

        public override bool Equals(object obj) => obj is GlobalHotKey hotKey && Equals(hotKey);

        public override int GetHashCode() => (int)Modifier ^ (int)Key;

        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString() => FullInfo();

        public string FullInfo() =>
            $"{Name} ; {HotKeyShared.CombineShortcut(Modifier, Key)} ; {(Enabled ? "" : "Not ")}Enabled ; GlobalHotKey";

        public static explicit operator string(GlobalHotKey toConvert) => toConvert.Name;

        public static explicit operator LocalHotKey(GlobalHotKey toConvert) => new(toConvert.Name, toConvert.Modifier, toConvert.Key, RaiseLocalEvent.OnKeyDown,
            toConvert.Enabled);

        protected virtual void OnHotKeyPress() {
            if (HotKeyPressed != null && Enabled)
                HotKeyPressed(this, new GlobalHotKeyEventArgs(this));
        }

        internal void RaiseOnHotKeyPressed() {
            OnHotKeyPress();
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("Name", Name);
            info.AddValue("Key", Key, typeof(Keys));
            info.AddValue("Modifiers", Modifier, typeof(Modifiers));
            info.AddValue("Enabled", Enabled);
        }

        #endregion
    }

    #endregion

    #region **LocalHotKey Class

    [Serializable]
    public class LocalHotKey : ISerializable, IEquatable<LocalHotKey>, IEquatable<ChordHotKey> {
        #region **Properties

        private string _name;
        private Keys _key;
        private RaiseLocalEvent _whenToRaise;
        private bool _enabled;
        private Modifiers _modifier;
        private bool _suppressKeyPress;
        private object _tag;

        public string Name {
            get => _name;
            private set => _name = value;
        }

        public Keys Key {
            get => _key;
            set => _key = value;
        }

        public bool Enabled {
            get => _enabled;
            set => _enabled = value;
        }

        public Modifiers Modifier {
            get => _modifier;
            set => _modifier = value;
        }

        public bool SuppressKeyPress {
            get => _suppressKeyPress;
            set => _suppressKeyPress = value;
        }

        public RaiseLocalEvent WhenToRaise {
            get => _whenToRaise;
            set => _whenToRaise = value;
        }

        public object Tag {
            get => _tag;
            set => _tag = value;
        }

        #endregion

        #region **Event Handlers

        public event LocalHotKeyEventHandler HotKeyPressed;

        #endregion

        #region **Constructors

        public LocalHotKey(string name, Keys key) :
            this(name, Modifiers.None, key) { }

        public LocalHotKey(string name, int key) :
            this(name, Modifiers.None, key) { }

        public LocalHotKey(string name, Keys key, RaiseLocalEvent whenToRaise) :
            this(name, Modifiers.None, key, whenToRaise, true) { }

        public LocalHotKey(string name, int key, RaiseLocalEvent whenToRaise) :
            this(name, Modifiers.None, key, whenToRaise, true) { }

        public LocalHotKey(string name, Modifiers modifiers, Keys key, RaiseLocalEvent whenToRaise) :
            this(name, modifiers, key, whenToRaise, true) { }

        public LocalHotKey(string name, Modifiers modifiers, int key, RaiseLocalEvent whenToRaise) :
            this(name, modifiers, key, whenToRaise, true) { }

        public LocalHotKey(string name, Keys key, RaiseLocalEvent whenToRaise, bool enabled) :
            this(name, Modifiers.None, key, whenToRaise, enabled) { }

        public LocalHotKey(string name, int key, RaiseLocalEvent whenToRaise, bool enabled) :
            this(name, Modifiers.None, key, whenToRaise, enabled) { }

        public LocalHotKey(string name, Modifiers modifiers, Keys key,
            RaiseLocalEvent whenToRaise = RaiseLocalEvent.OnKeyDown, bool enabled = true) {
            Name = name;
            Key = key;
            WhenToRaise = whenToRaise;
            Enabled = enabled;
            Modifier = modifiers;
        }

        public LocalHotKey(string name, Modifiers modifiers, int key,
            RaiseLocalEvent whenToRaise = RaiseLocalEvent.OnKeyDown, bool enabled = true) {
            Name = name;
            Key = (Keys)Enum.Parse(typeof(Keys), key.ToString());
            WhenToRaise = whenToRaise;
            Enabled = enabled;
            Modifier = modifiers;
        }

        protected LocalHotKey(SerializationInfo info, StreamingContext context) {
            Name = info.GetString("Name");
            Key = (Keys)info.GetValue("Key", typeof(Keys))!;
            WhenToRaise = (RaiseLocalEvent)info.GetValue("WTR", typeof(RaiseLocalEvent))!;
            Modifier = (Modifiers)info.GetValue("Modifiers", typeof(Modifiers))!;
            Enabled = info.GetBoolean("Enabled");
            SuppressKeyPress = info.GetBoolean("SuppressKeyPress");
        }

        #endregion

        #region **Events, Methods and Helpers

        public bool Equals(LocalHotKey other) {
            if (Key == other!.Key && Modifier == other.Modifier)
                return true;
            if (Name.ToLower() == other.Name.ToLower())
                return true;

            return false;
        }

        public bool Equals(ChordHotKey other) => (Key == other!.BaseKey && Modifier == other.BaseModifier);

        public override bool Equals(object obj) => obj is LocalHotKey hotKey
            ? Equals(hotKey)
            : obj is ChordHotKey chotKey && Equals(chotKey);


        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode() => (int)_whenToRaise ^ (int)_key;


        public override string ToString() => FullInfo();

        public string FullInfo() =>
            $"{Name} ; {HotKeyShared.CombineShortcut(Modifier, Key)} ; {WhenToRaise} ; {(Enabled ? "" : "Not ")}Enabled ; LocalHotKey";

        public static explicit operator string(LocalHotKey toConvert) => toConvert.Name;

        public static explicit operator GlobalHotKey(LocalHotKey toConvert) => new(toConvert.Name, toConvert.Modifier, toConvert.Key, toConvert.Enabled);

        protected virtual void OnHotKeyPress() {
            if (HotKeyPressed != null && Enabled)
                HotKeyPressed(this, new LocalHotKeyEventArgs(this));
        }

        internal void RaiseOnHotKeyPressed() {
            OnHotKeyPress();
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("Name", Name);
            info.AddValue("Key", Key, typeof(Keys));
            info.AddValue("Modifier", Modifier, typeof(Modifiers));
            info.AddValue("WTR", WhenToRaise, typeof(RaiseLocalEvent));
            info.AddValue("Enabled", Enabled);
            info.AddValue("SuppressKeyPress", SuppressKeyPress);
        }

        #endregion
    }

    #endregion

    #region **Hotkeys of Chord.

    [Serializable]
    public class ChordHotKey : ISerializable, IEquatable<ChordHotKey>, IEquatable<LocalHotKey> {
        #region **Properties.

        private string _name;
        private Keys _baseKey;
        private Keys _chordKey;
        private Modifiers _baseModifier;
        private Modifiers _chordModifier;
        private bool _enabled;
        private object _tag;

        public string Name {
            get => _name;
            private set => _name = value;
        }

        public Keys BaseKey {
            get => _baseKey;
            set => _baseKey = value;
        }

        public Keys ChordKey {
            get => _chordKey;
            set => _chordKey = value;
        }

        public Modifiers BaseModifier {
            get => _baseModifier;
            set {
                if (value != Modifiers.None)
                    _baseModifier = value;
                else
                    throw new ArgumentException("Cannot set BaseModifier to None.", nameof(value));
            }
        }

        public Modifiers ChordModifier {
            get => _chordModifier;
            set => _chordModifier = value;
        }


        public bool Enabled {
            get => _enabled;
            set => _enabled = value;
        }


        public Object Tag {
            get => _tag;
            set {
                if (_tag != null && _tag != value)
                    _tag = value;
            }
        }

        #endregion

        #region **Event Handlers.

        public event ChordHotKeyEventHandler HotKeyPressed;

        #endregion

        #region **Constructors

        public ChordHotKey(string name, Modifiers baseModifier, Keys baseKey, Modifiers chordModifier, Keys chordKey,
            bool enabled = true) {
            Name = name;
            BaseKey = baseKey;
            BaseModifier = baseModifier;
            ChordKey = chordKey;
            ChordModifier = chordModifier;
            Enabled = enabled;
        }

        public ChordHotKey(string name, Modifiers baseModifier, int baseKey, Modifiers chordModifier, int chordKey,
            bool enabled = true) {
            Name = name;
            BaseKey = (Keys)Enum.Parse(typeof(Keys), baseKey.ToString());
            BaseModifier = baseModifier;
            ChordKey = (Keys)Enum.Parse(typeof(Keys), chordKey.ToString());
            ChordModifier = chordModifier;
            Enabled = enabled;
        }

        public ChordHotKey(string name, Modifiers baseModifier, int baseKey, Modifiers chordModifier, Keys chordKey,
            bool enabled = true) {
            Name = name;
            BaseKey = (Keys)Enum.Parse(typeof(Keys), baseKey.ToString());
            BaseModifier = baseModifier;
            ChordKey = chordKey;
            ChordModifier = chordModifier;
            Enabled = enabled;
        }

        public ChordHotKey(string name, Modifiers baseModifier, Keys baseKey, Modifiers chordModifier, int chordKey,
            bool enabled = true) {
            Name = name;
            BaseKey = baseKey;
            BaseModifier = baseModifier;
            ChordKey = (Keys)Enum.Parse(typeof(Keys), chordKey.ToString());
            ChordModifier = chordModifier;
            Enabled = enabled;
        }

        public ChordHotKey(string name, Modifiers baseModifier, Keys baseKey, LocalHotKey chordHotKey,
            bool enabled = true) {
            Name = name;
            BaseKey = baseKey;
            BaseModifier = baseModifier;
            ChordKey = chordHotKey.Key;
            _chordModifier = chordHotKey.Modifier;
            Enabled = enabled;
        }

        protected ChordHotKey(SerializationInfo info, StreamingContext context) {
            Name = info.GetString("Name");
            BaseKey = (Keys)info.GetValue("BaseKey", typeof(Keys))!;
            BaseModifier = (Modifiers)info.GetValue("BaseModifier", typeof(Modifiers))!;
            ChordKey = (Keys)info.GetValue("ChordKey", typeof(Keys))!;
            ChordModifier = (Modifiers)info.GetValue("ChordModifier", typeof(Modifiers))!;
            Enabled = info.GetBoolean("Enabled");
        }

        #endregion

        #region **Events, Methods and Helpers

        public bool Equals(LocalHotKey other) => (BaseKey == other!.Key && BaseModifier == other.Modifier);

        public bool Equals(ChordHotKey other) {
            if (BaseKey == other!.BaseKey && BaseModifier == other.BaseModifier && ChordKey == other.ChordKey &&
                ChordModifier == other.ChordModifier)
                return true;

            if (Name.ToLower() == other.Name.ToLower())
                return true;

            return false;
        }

        public override bool Equals(object obj) => obj is LocalHotKey lhotKey
            ? Equals(lhotKey)
            : obj is ChordHotKey hotkey && Equals(hotkey);

        public override int GetHashCode() => (int)BaseKey ^ (int)ChordKey ^ (int)BaseModifier ^ (int)ChordModifier;

        public override string ToString() => FullInfo();

        public string FullInfo()
            =>
                $"{Name} ; {HotKeyShared.CombineShortcut(BaseModifier, BaseKey)} ; {HotKeyShared.CombineShortcut(ChordModifier, ChordKey)} ; {(Enabled ? "" : "Not ")}Enabled ; ChordHotKey";

        public string BaseInfo() => HotKeyShared.CombineShortcut(BaseModifier, BaseKey);

        public string ChordInfo() => HotKeyShared.CombineShortcut(ChordModifier, ChordKey);

        protected virtual void OnHotKeyPress() {
            if (HotKeyPressed != null && Enabled)
                HotKeyPressed(this, new ChordHotKeyEventArgs(this));
        }

        internal void RaiseOnHotKeyPressed() {
            OnHotKeyPress();
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("Name", Name);
            info.AddValue("BaseKey", BaseKey, typeof(Keys));
            info.AddValue("BaseModifier", BaseModifier, typeof(Modifiers));
            info.AddValue("ChordKey", ChordKey, typeof(Keys));
            info.AddValue("BaseModifier", ChordModifier, typeof(Modifiers));
            info.AddValue("Enabled", Enabled);
        }

        #endregion
    }

    #endregion

    #region **Enums and structs.

    [Flags]
    public enum Modifiers {
        None = 0x0000,

        Alt = 0x0001,

        Control = 0x0002,

        Shift = 0x0004,

        Win = 0x0008
    }

    public enum RaiseLocalEvent {
        OnKeyDown = 0x100,
        OnKeyUp = 0x101
    }

    internal enum KeyboardMessages {
        WmKeydown = 0x0100,

        WmKeyup = 0x0101,

        WmSyskeydown = 0x0104,

        WmSyskeyup = 0x0105,

        WmHotKey = 0x0312
    }

    internal enum KeyboardHookEnum {
        KeyboardHook = 0xD,
        KeyboardExtendedKey = 0x1,
        KeyboardKeyUp = 0x2
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KeyboardHookStruct {
        public readonly int VirtualKeyCode;
        public readonly int ScanCode;
        public readonly int Flags;
        public readonly int Time;
        public readonly int ExtraInfo;
    }

    public enum KeyboardEventNames {
        KeyDown,
        KeyUp
    }

    #endregion

    public delegate void GlobalHotKeyEventHandler(object sender, GlobalHotKeyEventArgs e);

    public delegate void LocalHotKeyEventHandler(object sender, LocalHotKeyEventArgs e);

    public delegate void PreChordHotkeyEventHandler(object sender, PreChordHotKeyEventArgs e);

    public delegate void ChordHotKeyEventHandler(object sender, ChordHotKeyEventArgs e);

    public delegate void HotKeyIsSetEventHandler(object sender, HotKeyIsSetEventArgs e);

    public delegate void HotKeyEventHandler(object sender, HotKeyEventArgs e);

    public delegate void KeyboardHookEventHandler(object sender, KeyboardHookEventArgs e);

        public class GlobalHotKeyEventArgs(GlobalHotKey hotKey) : EventArgs {
            public GlobalHotKey HotKey { get; private set; } = hotKey;
        }

        public class LocalHotKeyEventArgs(LocalHotKey hotKey) : EventArgs {
            public LocalHotKey HotKey { get; private set; } = hotKey;
        }

        public class PreChordHotKeyEventArgs(LocalHotKey hotkey) : EventArgs {
            public Keys BaseKey => hotkey.Key;
            public Modifiers BaseModifier => hotkey.Modifier;

            public bool HandleChord { get; set; }

            public override string ToString() => Info();

            public string Info() {
                string info = "";
                foreach (Modifiers mod in new HotKeyShared.ParseModifier((int)BaseModifier)) {
                    info += mod + " + ";
                }

                info += BaseKey.ToString();
                return info;
            }
        }

        public class ChordHotKeyEventArgs(ChordHotKey hotkey) : EventArgs {
            public ChordHotKey HotKey { get; private set; } = hotkey;
        }

        public class HotKeyIsSetEventArgs(Keys key, Modifiers modifier) : EventArgs {
            public Keys UserKey { get; private set; } = key;
            public Modifiers UserModifier { get; private set; } = modifier;
            public bool Cancel { get; set; }
            public string Shortcut => HotKeyShared.CombineShortcut(UserModifier, UserKey);
        }

        public class HotKeyEventArgs(Keys key, Modifiers modifier, RaiseLocalEvent keyPressEvent)
            : EventArgs {
            public Keys Key { get; private set; } = key;
            public Modifiers Modifier { get; private set; } = modifier;
            public RaiseLocalEvent KeyPressEvent { get; private set; } = keyPressEvent;
        }

        public class KeyboardHookEventArgs : EventArgs {
            public KeyboardHookEventArgs(KeyboardHookStruct lparam) => LParam = lparam;

            private readonly KeyboardHookStruct _lParam;
            private bool _handled;

            private KeyboardHookStruct LParam {
                get => _lParam;
                init {
                    _lParam = value;
                    var nonVirtual = Win32.MapVirtualKey((uint)VirtualKeyCode, 2);
                    Char = Convert.ToChar(nonVirtual);
                }
            }

            public int VirtualKeyCode => LParam.VirtualKeyCode;

            public Keys Key => (Keys)VirtualKeyCode;

            public char Char { get; private set; }

            public string KeyString {
                get {
                    if (Char == '\0') {
                        return Key == Keys.Return ? "[Enter]" : $"[{Key}]";
                    }

                    if (Char == '\r') {
                        Char = '\0';
                        return "[Enter]";
                    }

                    if (Char == '\b') {
                        Char = '\0';
                        return "[Backspace]";
                    }

                    return Char.ToString(CultureInfo.InvariantCulture);
                }
            }

            public bool Handled {
                get => _handled;
                set {
                    if (KeyboardEventName != KeyboardEventNames.KeyUp)
                        _handled = value;
                }
            }

            public KeyboardEventNames KeyboardEventName { get; internal set; }

            public enum Modifiers {
                None,
                Shift,
                Control,
                Alt,
                ShiftControl,
                ShiftAlt,
                ControlAlt,
                ShiftControlAlt
            }
            
            public Modifiers Modifier {
                get {
                    var keyBoard = new Microsoft.VisualBasic.Devices.Keyboard();
                    return keyBoard.AltKeyDown switch {
                        true when keyBoard.CtrlKeyDown && keyBoard.ShiftKeyDown => Modifiers.ShiftControlAlt,
                        true when keyBoard.CtrlKeyDown && !keyBoard.ShiftKeyDown => Modifiers.ControlAlt,
                        true when !keyBoard.CtrlKeyDown && keyBoard.ShiftKeyDown => Modifiers.ShiftAlt,
                        false when keyBoard.CtrlKeyDown && keyBoard.ShiftKeyDown => Modifiers.ShiftControl,
                        false when !keyBoard.CtrlKeyDown && keyBoard.ShiftKeyDown => Modifiers.Shift,
                        true when !keyBoard.CtrlKeyDown && !keyBoard.ShiftKeyDown => Modifiers.Alt,
                        false when keyBoard.CtrlKeyDown && !keyBoard.ShiftKeyDown => Modifiers.Control,
                        _ => Modifiers.None
                    };
                }
            }

            public int Time => _lParam.Time;
        }

    
}