using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Windows.Forms;


namespace Xpand.XAF.Modules.Windows.SystemActions {
    #region **GlobalHotKey Class

    /// <summary>Initializes a new instance of this class.
    /// </summary>
    [Serializable]
    public class GlobalHotKey : INotifyPropertyChanged, ISerializable, IEquatable<GlobalHotKey> {
        #region **Properties

        private string _name;
        private Keys _key;
        private Modifiers _modifier;
        private bool _enabled;
        private object _tag;

        /// <summary>The id this hotkey is registered with, if it has been registered.
        /// </summary>
        public int Id { get; internal set; }

        /// <summary>A unique name for the HotKey.
        /// </summary>
        public string Name {
            get => _name;
            private set {
                if (_name != value)
                    if (HotKeyShared.IsValidHotkeyName(value)) {
                        _name = value;
                    }
                    else {
                        throw new HotKeyInvalidNameException("the HotKeyname '" + value + "' is invalid");
                    }
            }
        }

        /// <summary>The Key for the HotKey.
        /// </summary>
        public Keys Key {
            get => _key;
            set {
                if (_key != value) {
                    _key = value;
                    OnPropertyChanged("Key");
                }
            }
        }

        ///<summary> The modifier. Multiple modifiers can be combined with or.
        /// </summary>
        public Modifiers Modifier {
            get => _modifier;
            set {
                if (_modifier != value) {
                    _modifier = value;
                    OnPropertyChanged("Modifier");
                }
            }
        }

        /// <summary>Determines if the Hotkey is active.
        /// </summary>
        public bool Enabled {
            get => _enabled;
            set {
                if (value != _enabled) {
                    _enabled = value;
                    OnPropertyChanged("Enabled");
                }
            }
        }

        /// <summary>Gets or Sets the object that contains data about the control.
        /// </summary>
        public object Tag {
            get => _tag;
            set => _tag = value;
        }

        #endregion

        #region **Event Handlers

        /// <summary>Raised when a property of this Hotkey is changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Will be raised if this hotkey is pressed (works only if registered in the HotKeyManager.)
        /// </summary>
        public event GlobalHotKeyEventHandler HotKeyPressed;

        #endregion

        #region **Constructor

        /// <summary>Creates a GlobalHotKey object. This instance has to be registered in a HotKeyManager.
        /// </summary>
        /// <param name="name">The unique identifier for this GlobalHotKey.</param>
        /// <param name="key">The key to be registered.</param>
        /// <param name="modifier">The modifier for this key. Multiple modifiers can be combined with or.</param>
        /// <param name="enabled">Specifies if event for this GlobalHotKey should be raised.</param>
        public GlobalHotKey(string name, Modifiers modifier, Keys key, bool enabled = true) {
            Name = name;
            Key = key;
            Modifier = modifier;
            Enabled = enabled;
        }

        /// <summary>Creates a GlobalHotKey object. This instance has to be registered in a HotKeyManager.
        /// </summary>
        /// <param name="name">The unique identifier for this GlobalHotKey.</param>
        /// <param name="key">The key to be registered.</param>
        /// <param name="modifier">The modifier for this key. Multiple modifiers can be combined with or.</param>
        /// <param name="enabled">Specifies if event for this GlobalHotKey should be raised.</param>
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

        /// <summary>Compares a GlobalHotKey to another.
        /// </summary>
        /// <param name="other">The GlobalHotKey to compare.</param>
        /// <returns>True if the HotKey is equal and false if otherwise.</returns>
        public bool Equals(GlobalHotKey other) =>
            Key == other!.Key && Modifier == other.Modifier || Name.ToLower() == other.Name.ToLower();

        //Override .Equals(object)
        public override bool Equals(object obj) => obj is GlobalHotKey hotKey && Equals(hotKey);

        //Override .GetHashCode of this object.
        public override int GetHashCode() {
            return (int)Modifier ^ (int)Key;
        }

        //To determine if a property of the GlobalHotkey has changed.
        protected virtual void OnPropertyChanged(string propertyName) {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        //Override the .ToString()        
        public override string ToString() {
            return FullInfo();
        }

        /// <summary>Information about this Hotkey.
        /// </summary>
        /// <returns>The information about this, delimited by ';'</returns>
        public string FullInfo() =>
            $"{Name} ; {HotKeyShared.CombineShortcut(Modifier, Key)} ; {(Enabled ? "" : "Not ")}Enabled ; GlobalHotKey";

        //Can use (string)GlobalHotKey.
        /// <summary>Converts the GlobalHotKey to a string.
        /// </summary>
        /// <param name="toConvert">The Hotkey to convert.</param>
        /// <returns>The string Name of the GlobalHotKey.</returns>
        public static explicit operator string(GlobalHotKey toConvert) {
            return toConvert.Name;
        }

        //Can use (LocalHotKey)GlobalHotKey.
        /// <summary>Converts the GlobalHotKey to a LocalHotKey
        /// </summary>
        /// <param name="toConvert">The GlobalHotKey to convert.</param>
        /// <returns>a LocalHotKey of the GlobalHotKey.</returns>
        public static explicit operator LocalHotKey(GlobalHotKey toConvert) {
            return new LocalHotKey(toConvert.Name, toConvert.Modifier, toConvert.Key, RaiseLocalEvent.OnKeyDown,
                toConvert.Enabled);
        }

        /// <summary>The Event raised the hotkey is pressed.
        /// </summary>
        protected virtual void OnHotKeyPress() {
            if (HotKeyPressed != null && Enabled)
                HotKeyPressed(this, new GlobalHotKeyEventArgs(this));
        }

        /// <summary>Raises the GlobalHotKey Pressed event.
        /// </summary>
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

    /// <summary>Initializes a new instance of this class.
    /// </summary>
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

        /// <summary>The Unique id for this HotKey.
        /// </summary>
        public string Name {
            get => _name;
            private set {
                if (_name != value)
                    if (HotKeyShared.IsValidHotkeyName(value))
                        _name = value;
                    else
                        throw new HotKeyInvalidNameException("the HotKeyname '" + value + "' is invalid");
            }
        }

        /// <summary>Gets or sets the key to register.
        /// </summary>
        public Keys Key {
            get => _key;
            set => _key = value;
        }

        /// <summary>Determines if the HotKey is active.
        /// </summary>
        public bool Enabled {
            get => _enabled;
            set => _enabled = value;
        }

        /// <summary>Gets or sets the modifiers for this hotKey, multiple modifiers can be combined with "Xor"
        /// </summary>
        public Modifiers Modifier {
            get => _modifier;
            set => _modifier = value;
        }

        /// <summary>Gets or sets a value whether the key event should be passed on to the underlying control.
        /// </summary>
        public bool SuppressKeyPress {
            get => _suppressKeyPress;
            set => _suppressKeyPress = value;
        }

        /// <summary>Specifies when the event for this key should be raised.
        /// </summary>
        public RaiseLocalEvent WhenToRaise {
            get => _whenToRaise;
            set => _whenToRaise = value;
        }

        /// <summary>Gets or Sets the object that contains data about the Hotkey.
        /// </summary>
        public object Tag {
            get => _tag;
            set => _tag = value;
        }

        #endregion

        #region **Event Handlers

        /// <summary>Will be raised if this hotkey is pressed (works only if registered in the HotKeyManager.)
        /// </summary>
        public event LocalHotKeyEventHandler HotKeyPressed;

        #endregion

        #region **Constructors

        /// <summary>Creates a LocalHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this LocalHotKey.</param>
        /// <param name="key">The key to be registered.</param>
        public LocalHotKey(string name, Keys key) :
            this(name, Modifiers.None, key) { }

        /// <summary>Creates a LocalHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this LocalHotKey.</param>
        /// <param name="key">The key to be registered.</param>
        public LocalHotKey(string name, int key) :
            this(name, Modifiers.None, key) { }

        /// <summary>Creates a LocalHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this LocalHotKey.</param>
        /// <param name="key">The key to be registered.</param>
        /// <param name="whenToRaise">Specifies when the event should be raised.</param>
        public LocalHotKey(string name, Keys key, RaiseLocalEvent whenToRaise) :
            this(name, Modifiers.None, key, whenToRaise, true) { }

        /// <summary>Creates a LocalHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this LocalHotKey.</param>
        /// <param name="key">The key to be registered.</param>
        /// <param name="whenToRaise">Specifies when the event should be raised.</param>
        public LocalHotKey(string name, int key, RaiseLocalEvent whenToRaise) :
            this(name, Modifiers.None, key, whenToRaise, true) { }

        /// <summary>Creates a LocalHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this LocalHotKey.</param>
        /// <param name="key">The key to register.</param>
        /// <param name="modifiers">The modifier for this key, multiple modifiers can be combined with Xor</param>
        /// <param name="whenToRaise">Specifies when the event should be raised.</param>
        public LocalHotKey(string name, Modifiers modifiers, Keys key, RaiseLocalEvent whenToRaise) :
            this(name, modifiers, key, whenToRaise, true) { }

        /// <summary>Creates a LocalHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this LocalHotKey.</param>
        /// <param name="key">The key to register.</param>
        /// <param name="modifiers">The modifier for this key, multiple modifiers can be combined with Xor</param>
        /// <param name="whenToRaise">Specifies when the event should be raised.</param>
        public LocalHotKey(string name, Modifiers modifiers, int key, RaiseLocalEvent whenToRaise) :
            this(name, modifiers, key, whenToRaise, true) { }

        /// <summary>Creates a LocalHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this LocalHotKey</param>
        /// <param name="key">The key to register.</param>
        /// <param name="whenToRaise">Specifies when the event should be raised.</param>
        /// <param name="enabled">Specifies if event for this GlobalHotKey should be raised.</param>
        public LocalHotKey(string name, Keys key, RaiseLocalEvent whenToRaise, bool enabled) :
            this(name, Modifiers.None, key, whenToRaise, enabled) { }

        /// <summary>Creates a LocalHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this LocalHotKey</param>
        /// <param name="key">The key to register.</param>
        /// <param name="whenToRaise">Specifies when the event should be raised.</param>
        /// <param name="enabled">Specifies if event for this GlobalHotKey should be raised.</param>
        public LocalHotKey(string name, int key, RaiseLocalEvent whenToRaise, bool enabled) :
            this(name, Modifiers.None, key, whenToRaise, enabled) { }

        /// <summary>Creates a LocalHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this LocalHotKey</param>
        /// <param name="key">The key to register.</param>
        /// <param name="modifiers">The modifier for this key, multiple modifiers can be combined with Xor</param>
        /// <param name="whenToRaise">Specifies when the event should be raised.</param>
        /// <param name="enabled">Specifies if event for this GlobalHotKey should be raised.</param>
        public LocalHotKey(string name, Modifiers modifiers, Keys key,
            RaiseLocalEvent whenToRaise = RaiseLocalEvent.OnKeyDown, bool enabled = true) {
            //if (modifiers == Win.Modifiers.Win) { throw new InvalidOperationException("Window Key cannot be used as modifier for Local HotKeys"); }
            Name = name;
            Key = key;
            WhenToRaise = whenToRaise;
            Enabled = enabled;
            Modifier = modifiers;
        }

        /// <summary>Creates a LocalHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this LocalHotKey</param>
        /// <param name="key">The key to register.</param>
        /// <param name="modifiers">The modifier for this key, multiple modifiers can be combined with Xor</param>
        /// <param name="whenToRaise">Specifies when the event should be raised.</param>
        /// <param name="enabled">Specifies if event for this GlobalHotKey should be raised.</param>
        public LocalHotKey(string name, Modifiers modifiers, int key,
            RaiseLocalEvent whenToRaise = RaiseLocalEvent.OnKeyDown, bool enabled = true) {
            //if (modifiers == Modifiers.Win) { throw new InvalidOperationException("Window Key cannot be used as modifier for Local HotKeys"); }
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

        /// <summary>Compares a LocalHotKey to another.
        /// </summary>
        /// <param name="other">The LocalHotKey to compare.</param>
        /// <returns>True if the HotKey is equal and false if otherwise.</returns>
        public bool Equals(LocalHotKey other) {
            //We'll be comparing the Key, Modifier and the Name.
            if (Key == other!.Key && Modifier == other.Modifier)
                return true;
            if (Name.ToLower() == other.Name.ToLower())
                return true;

            return false;
        }

        /// <summary>Compares a LocalHotKey to a ChordHotKey.
        /// </summary>
        /// <param name="other">The ChordHotKey to compare.</param>
        /// <returns>True if equal, false otherwise.</returns>
        public bool Equals(ChordHotKey other) {
            return (Key == other!.BaseKey && Modifier == other.BaseModifier);
        }

        //Override .Equals(object)
        public override bool Equals(object obj) => obj is LocalHotKey hotKey
            ? Equals(hotKey)
            : obj is ChordHotKey chotKey && Equals(chotKey);


        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode() => (int)_whenToRaise ^ (int)_key;


        public override string ToString() => FullInfo();

        /// <summary>Information about this Hotkey.
        /// </summary>
        /// <returns>The properties of the hotkey.</returns>
        public string FullInfo() =>
            $"{Name} ; {HotKeyShared.CombineShortcut(Modifier, Key)} ; {WhenToRaise} ; {(Enabled ? "" : "Not ")}Enabled ; LocalHotKey";

        //Can use (string)LocalHotKey.
        /// <summary>Converts the LocalHotKey to a string.
        /// </summary>
        /// <param name="toConvert">The Hotkey to convert.</param>
        /// <returns>The string Name of the LocalHotKey.</returns>
        public static explicit operator string(LocalHotKey toConvert) {
            return toConvert.Name;
        }

        /// <summary>Converts a LocalHotKey to a GlobalHotKey.
        /// </summary>
        /// <param name="toConvert">The LocalHotKey to convert.</param>
        /// <returns>an instance of the GlobalHotKey.</returns>
        public static explicit operator GlobalHotKey(LocalHotKey toConvert) {
            return new GlobalHotKey(toConvert.Name, toConvert.Modifier, toConvert.Key, toConvert.Enabled);
        }

        /// <summary>The Event raised the hotkey is pressed.
        /// </summary>
        protected virtual void OnHotKeyPress() {
            if (HotKeyPressed != null && Enabled)
                HotKeyPressed(this, new LocalHotKeyEventArgs(this));
        }

        /// <summary>Raises the HotKeyPressed event.
        /// </summary>
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

    /// <summary>Initializes a new instance of this class.
    /// Register multiple shortcuts like Control + \, Control + N.
    /// </summary>
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

        /// <summary>The unique id for this key
        /// </summary>
        public string Name {
            get => _name;
            private set {
                if (_name != value)
                    if (HotKeyShared.IsValidHotkeyName(value))
                        _name = value;
                    else
                        throw new HotKeyInvalidNameException("the HotKeyname '" + value + "' is invalid");
            }
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

        /// <summary>Will be raised if this hotkey is pressed.
        /// The event is raised if the basic key and basic modifier and the chord key and modifier is pressed.
        /// Works only if registered in the HotKeyManager.
        /// </summary>
        public event ChordHotKeyEventHandler HotKeyPressed;

        #endregion

        #region **Constructors

        /// <summary>Creates a ChordHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this ChordHotKey.</param>
        /// <param name="baseKey">The key to start the chord.</param>
        /// <param name="baseModifier">The modifier associated with the base key.</param>
        /// <param name="chordKey">The key of chord.</param>
        /// <param name="chordModifier">The modifier associated with the Key of chord</param>
        /// <param name="enabled">Specifies if this hotkey is active</param>
        public ChordHotKey(string name, Modifiers baseModifier, Keys baseKey, Modifiers chordModifier, Keys chordKey,
            bool enabled = true) {
            Name = name;
            BaseKey = baseKey;
            BaseModifier = baseModifier;
            ChordKey = chordKey;
            ChordModifier = chordModifier;
            Enabled = enabled;
        }

        /// <summary>Creates a ChordHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this ChordHotKey.</param>
        /// <param name="baseKey">The key to start the chord.</param>
        /// <param name="baseModifier">The modifier associated with the base key.</param>
        /// <param name="chordKey">The key of chord.</param>
        /// <param name="chordModifier">The modifier associated with the Key of chord</param>
        /// <param name="enabled">Specifies if this hotkey is active</param>
        public ChordHotKey(string name, Modifiers baseModifier, int baseKey, Modifiers chordModifier, int chordKey,
            bool enabled = true) {
            Name = name;
            BaseKey = (Keys)Enum.Parse(typeof(Keys), baseKey.ToString());
            BaseModifier = baseModifier;
            ChordKey = (Keys)Enum.Parse(typeof(Keys), chordKey.ToString());
            ChordModifier = chordModifier;
            Enabled = enabled;
        }

        /// <summary>Creates a ChordHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this ChordHotKey.</param>
        /// <param name="baseKey">The key to start the chord.</param>
        /// <param name="baseModifier">The modifier associated with the base key.</param>
        /// <param name="chordKey">The key of chord.</param>
        /// <param name="chordModifier">The modifier associated with the Key of chord.</param>
        /// <param name="enabled">Specifies if this hotkey is active.</param>
        public ChordHotKey(string name, Modifiers baseModifier, int baseKey, Modifiers chordModifier, Keys chordKey,
            bool enabled = true) {
            Name = name;
            BaseKey = (Keys)Enum.Parse(typeof(Keys), baseKey.ToString());
            BaseModifier = baseModifier;
            ChordKey = chordKey;
            ChordModifier = chordModifier;
            Enabled = enabled;
        }

        /// <summary>Creates a ChordHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this ChordHotKey.</param>
        /// <param name="baseKey">The key to start the chord.</param>
        /// <param name="baseModifier">The modifier associated with the base key.</param>
        /// <param name="chordKey">The key of chord.</param>
        /// <param name="chordModifier">The modifier associated with the Key of chord.</param>
        /// <param name="enabled">Specifies if this hotkey is active.</param>
        public ChordHotKey(string name, Modifiers baseModifier, Keys baseKey, Modifiers chordModifier, int chordKey,
            bool enabled = true) {
            Name = name;
            BaseKey = baseKey;
            BaseModifier = baseModifier;
            ChordKey = (Keys)Enum.Parse(typeof(Keys), chordKey.ToString());
            ChordModifier = chordModifier;
            Enabled = enabled;
        }

        /// <summary>Creates a ChordHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this ChordHotKey.</param>
        /// <param name="baseKey">The key to start the chord.</param>
        /// <param name="baseModifier">The modifier associated with the base key.</param>
        /// <param name="chordHotKey">The LocalHotKey object to extract the chord key and modifier from.</param>
        /// <param name="enabled">Specifies that the hotkey is active,</param>
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

        /// <summary>Compares this HotKey to another LocalHotKey.
        /// </summary>
        /// <param name="other">The LocalHotKey to compare.</param>
        /// <returns>True if equal, false otherwise.</returns>
        public bool Equals(LocalHotKey other) => (BaseKey == other!.Key && BaseModifier == other.Modifier);

        /// <summary>Compares this Hotkey to another ChordHotKey.
        /// </summary>
        /// <param name="other">The ChordHotKey to compare.</param>
        /// <returns>True if equal, false otherwise.</returns>
        public bool Equals(ChordHotKey other) {
            if (BaseKey == other!.BaseKey && BaseModifier == other.BaseModifier && ChordKey == other.ChordKey &&
                ChordModifier == other.ChordModifier)
                return true;

            if (Name.ToLower() == other.Name.ToLower())
                return true;

            return false;
        }

        /// <summary>Checks if this Hotkey is equal to another ChordHotkey or LocalHotkey.
        /// </summary>
        /// <param name="obj">The Hotkey to compare</param>
        /// <returns>True if equal, false otherwise.</returns>
        public override bool Equals(object obj) => obj is LocalHotKey lhotKey
            ? Equals(lhotKey)
            : obj is ChordHotKey hotkey && Equals(hotkey);

        /// <summary>Serves the hash function for this class.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => (int)BaseKey ^ (int)ChordKey ^ (int)BaseModifier ^ (int)ChordModifier;

        /// <summary>Converts the HotKey to a string.
        /// </summary>
        /// <returns>The FullInfo of the HotKey.</returns>
        public override string ToString() => FullInfo();

        /// <summary>Specifies the entire information about this HotKey.
        /// </summary>
        /// <returns>A string representation of the information.</returns>
        public string FullInfo()
            =>
                $"{Name} ; {HotKeyShared.CombineShortcut(BaseModifier, BaseKey)} ; {HotKeyShared.CombineShortcut(ChordModifier, ChordKey)} ; {(Enabled ? "" : "Not ")}Enabled ; ChordHotKey";

        /// <summary>Specifies the base information of this HotKey.
        /// </summary>
        /// <returns>A string representation of the information.</returns>
        public string BaseInfo() {
            return HotKeyShared.CombineShortcut(BaseModifier, BaseKey);
        }

        /// <summary>Specifies the Chord information of this HotKey.
        /// </summary>
        /// <returns>A string representation of the information.</returns>
        public string ChordInfo() {
            return HotKeyShared.CombineShortcut(ChordModifier, ChordKey);
        }

        /// <summary>The Event raised when the hotkey is pressed.
        /// </summary>
        protected virtual void OnHotKeyPress() {
            if (HotKeyPressed != null && Enabled)
                HotKeyPressed(this, new ChordHotKeyEventArgs(this));
        }

        /// <summary>Raises the HotKeyPressed event.
        /// </summary>
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

    /// <summary>Defines the key to use as Modifier.
    /// </summary>
    [Flags]
    public enum Modifiers {
        /// <summary>Specifies that the key should be treated as is, without any modifier.
        /// </summary>
        None = 0x0000,

        /// <summary>Specifies that the Accelerator key (ALT) is pressed with the key.
        /// </summary>
        Alt = 0x0001,

        /// <summary>Specifies that the Control key is pressed with the key.
        /// </summary>
        Control = 0x0002,

        /// <summary>Specifies that the Shift key is pressed with the associated key.
        /// </summary>
        Shift = 0x0004,

        /// <summary>Specifies that the Window key is pressed with the associated key.
        /// </summary>
        Win = 0x0008
    }

    public enum RaiseLocalEvent {
        OnKeyDown = 0x100, //Also 256. Same as WM_KEYDOWN.
        OnKeyUp = 0x101 //Also 257, Same as WM_KEYUP.
    }

    internal enum KeyboardMessages {
        /// <summary>A key is down.
        /// </summary>
        WmKeydown = 0x0100,

        /// <summary>A key is released.
        /// </summary>
        WmKeyup = 0x0101,

        /// <summary> Same as KeyDown but captures keys pressed after Alt.
        /// </summary>
        WmSyskeydown = 0x0104,

        /// <summary>Same as KeyUp but captures keys pressed after Alt.
        /// </summary>
        WmSyskeyup = 0x0105,

        /// <summary> When a hotkey is pressed.
        /// </summary>
        WmHotKey = 0x0312
    }

    internal enum KeyboardHookEnum {
        KeyboardHook = 0xD,
        KeyboardExtendedKey = 0x1,
        KeyboardKeyUp = 0x2
    }

    /// <summary>
    /// The KBDLLHOOKSTRUCT structure contains information about a low-level keyboard input event. 
    /// </summary>
    /// <remarks>
    /// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/winui/winui/windowsuserinterface/windowing/hooks/hookreference/hookstructures/cwpstruct.asp
    /// </remarks>
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

    /// <summary>Represents the method that will handle a HotKeyManagement LocalHotKeyPressed event
    /// </summary>
    public delegate void LocalHotKeyEventHandler(object sender, LocalHotKeyEventArgs e);

    /// <summary>Represents the method that will handle a HotKeyManagement PreChordStarted event
    /// </summary>
    public delegate void PreChordHotkeyEventHandler(object sender, PreChordHotKeyEventArgs e);

    /// <summary>Represents the method that will handle a HotKeyManagement ChordHotKeyPressed event
    /// </summary>
    public delegate void ChordHotKeyEventHandler(object sender, ChordHotKeyEventArgs e);

    /// <summary>Represents the method that will handle a HotKeyManagement HotKeyIsSet event
    /// </summary>
    public delegate void HotKeyIsSetEventHandler(object sender, HotKeyIsSetEventArgs e);

    /// <summary>Represents the method that will handle a HotKeyManagement HotKeyPressed event
    /// </summary>
    public delegate void HotKeyEventHandler(object sender, HotKeyEventArgs e);

    /// <summary>Represents the method that will handle a HotKeyManagement KeyboardHook event
    /// </summary>
    public delegate void KeyboardHookEventHandler(object sender, KeyboardHookEventArgs e);

        public class GlobalHotKeyEventArgs : EventArgs {
            public GlobalHotKey HotKey { get; private set; }

            public GlobalHotKeyEventArgs(GlobalHotKey hotKey) {
                HotKey = hotKey;
            }
        }

        public class LocalHotKeyEventArgs : EventArgs {
            public LocalHotKey HotKey { get; private set; }

            public LocalHotKeyEventArgs(LocalHotKey hotKey) {
                HotKey = hotKey;
            }
        }

        public class PreChordHotKeyEventArgs : EventArgs {
            private readonly LocalHotKey _hotKey;

            public Keys BaseKey => _hotKey.Key;
            public Modifiers BaseModifier => _hotKey.Modifier;

            public bool HandleChord { get; set; }

            /// <summary>Displays information about
            /// </summary>
            public override string ToString() {
                return Info();
            }

            /// <summary>Displays the Modifier and key in extended format.
            /// </summary>
            /// <returns>The key and modifier in string.</returns>
            public string Info() {
                string info = "";
                foreach (Modifiers mod in new HotKeyShared.ParseModifier((int)BaseModifier)) {
                    info += mod + " + ";
                }

                info += BaseKey.ToString();
                return info;
            }

            public PreChordHotKeyEventArgs(LocalHotKey hotkey) {
                _hotKey = hotkey;
            }
        }

        public class ChordHotKeyEventArgs : EventArgs {
            /// <summary>The HotKey that raised this event.
            /// </summary>
            public ChordHotKey HotKey { get; private set; }

            public ChordHotKeyEventArgs(ChordHotKey hotkey) {
                HotKey = hotkey;
            }
        }

        public class HotKeyIsSetEventArgs : EventArgs {
            public Keys UserKey { get; private set; }
            public Modifiers UserModifier { get; private set; }
            public bool Cancel { get; set; }
            public string Shortcut => HotKeyShared.CombineShortcut(UserModifier, UserKey);

            public HotKeyIsSetEventArgs(Keys key, Modifiers modifier) {
                UserKey = key;
                UserModifier = modifier;
            }
        }

        public class HotKeyEventArgs : EventArgs {
            public Keys Key { get; private set; }
            public Modifiers Modifier { get; private set; }
            public RaiseLocalEvent KeyPressEvent { get; private set; }

            public HotKeyEventArgs(Keys key, Modifiers modifier, RaiseLocalEvent keyPressEvent) {
                Key = key;
                Modifier = modifier;
                KeyPressEvent = keyPressEvent;
            }
        }

        public class KeyboardHookEventArgs : EventArgs {
            public KeyboardHookEventArgs(KeyboardHookStruct lparam) {
                LParam = lparam;
            }

            private KeyboardHookStruct _lParam;
            private bool _handled;

            private KeyboardHookStruct LParam {
                get => _lParam;
                set {
                    _lParam = value;
                    var nonVirtual = Win32.MapVirtualKey((uint)VirtualKeyCode, 2);
                    Char = Convert.ToChar(nonVirtual);
                }
            }

            /// <summary>The ASCII code of the key pressed.
            /// </summary>
            public int VirtualKeyCode => LParam.VirtualKeyCode;

            /// <summary>The Key pressed.
            /// </summary>
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

            /// <summary>Specifies if this key should be processed  by other windows.
            /// </summary>
            public bool Handled {
                get => _handled;
                set {
                    if (KeyboardEventName != KeyboardEventNames.KeyUp)
                        _handled = value;
                }
            }

            /// <summary>The event that raised this 'event' Whether KeyUp or KeyDown.
            /// </summary>
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