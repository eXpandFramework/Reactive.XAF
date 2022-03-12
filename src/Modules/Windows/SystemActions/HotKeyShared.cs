using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Xpand.XAF.Modules.Windows.SystemActions {
    public static class HotKeyShared {
        


        /// <summary>Parses a shortcut string like 'Control + Alt + Shift + V' and returns the key and modifiers.
        /// </summary>
        /// <param name="text">The shortcut string to parse.</param>
        /// <param name="separator">The delimiter for the shortcut.</param>
        /// <returns>The Modifier in the lower bound and the key in the upper bound.</returns>
        public static object[] ParseShortcut(string text, string separator) {
            bool hasAlt = false;
            bool hasControl = false;
            bool hasShift = false;
            bool hasWin = false;

            Modifiers modifier = Modifiers.None;
            int current = 0;

            string[] separators = new string[] { separator };
            var result = text.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            //Iterate through the keys and find the modifier.
            foreach (string entry in result) {
                //Find the Control Key.
                if (entry.Trim() == Keys.Control.ToString()) {
                    hasControl = true;
                }

                //Find the Alt key.
                if (entry.Trim() == Keys.Alt.ToString()) {
                    hasAlt = true;
                }

                //Find the Shift key.
                if (entry.Trim() == Keys.Shift.ToString()) {
                    hasShift = true;
                }

                //Find the Window key.
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

            KeysConverter keyconverter = new KeysConverter();
            var key = (Keys)keyconverter.ConvertFrom(result.GetValue(result.Length - 1))!;

            return new object[] { modifier, key };
        }

        /// <summary>Combines the modifier and key to a shortcut.
        /// Changes Control;Shift;Alt;T to Control + Shift + Alt + T
        /// </summary>
        /// <param name="mod">The modifier.</param>
        /// <param name="key">The key.</param>
        /// <returns>A string representation of the modifier and key.</returns>
        public static string CombineShortcut(Modifiers mod, Keys key) {
            string hotkey = "";
            foreach (Modifiers a in new ParseModifier((int)mod)) {
                hotkey += a.ToString() + " + ";
            }

            if (hotkey.Contains(Modifiers.None.ToString())) hotkey = "";
            hotkey += key.ToString();
            return hotkey;
        }

        /// <summary>Combines the modifier and key to a shortcut.
        /// Changes Control;Shift;Alt; to Control + Shift + Alt
        /// </summary>
        /// <param name="mod">The modifier.</param>
        /// <returns>A string representation of the modifier</returns>
        public static string CombineShortcut(Modifiers mod) {
            string hotkey = "";
            foreach (Modifiers a in new ParseModifier((int)mod)) {
                hotkey += a.ToString() + " + ";
            }

            if (hotkey.Contains(Modifiers.None.ToString())) hotkey = "";
            if (hotkey.Trim().EndsWith("+")) hotkey = hotkey.Trim().Substring(0, hotkey.Length - 1);

            return hotkey;
        }

        /// <summary>Allows the conversion of an integer to its modifier representation.
        /// </summary>
        public struct ParseModifier : IEnumerable {
            private readonly List<Modifiers> _enumeration;
            public bool HasAlt;
            public bool HasControl;
            public bool HasShift;
            public bool HasWin;

            /// <summary>Initializes this class.
            /// </summary>
            /// <param name="modifier">The integer representation of the modifier to parse.</param>
            public ParseModifier(int modifier) {
                _enumeration = new List<Modifiers>();
                HasAlt = false;
                HasWin = false;
                HasShift = false;
                HasControl = false;
                switch (modifier) {
                    case 0:
                        _enumeration.Add(Modifiers.None);
                        break;
                    case 1:
                        HasAlt = true;
                        _enumeration.Add(Modifiers.Alt);
                        break;
                    case 2:
                        HasControl = true;
                        _enumeration.Add(Modifiers.Control);
                        break;
                    case 3:
                        HasAlt = true;
                        HasControl = true;
                        _enumeration.Add(Modifiers.Control);
                        _enumeration.Add(Modifiers.Alt);
                        break;
                    case 4:
                        HasShift = true;
                        _enumeration.Add(Modifiers.Shift);
                        break;
                    case 5:
                        HasShift = true;
                        HasAlt = true;
                        _enumeration.Add(Modifiers.Shift);
                        _enumeration.Add(Modifiers.Alt);
                        break;
                    case 6:
                        HasShift = true;
                        HasControl = true;
                        _enumeration.Add(Modifiers.Shift);
                        _enumeration.Add(Modifiers.Control);
                        break;
                    case 7:
                        HasControl = true;
                        HasShift = true;
                        HasAlt = true;
                        _enumeration.Add(Modifiers.Shift);
                        _enumeration.Add(Modifiers.Control);
                        _enumeration.Add(Modifiers.Alt);
                        break;
                    case 8:
                        HasWin = true;
                        _enumeration.Add(Modifiers.Win);
                        break;
                    case 9:
                        HasAlt = true;
                        HasWin = true;
                        _enumeration.Add(Modifiers.Alt);
                        _enumeration.Add(Modifiers.Win);
                        break;
                    case 10:
                        HasControl = true;
                        HasWin = true;
                        _enumeration.Add(Modifiers.Control);
                        _enumeration.Add(Modifiers.Win);
                        break;
                    case 11:
                        HasControl = true;
                        HasAlt = true;
                        HasWin = true;
                        _enumeration.Add(Modifiers.Control);
                        _enumeration.Add(Modifiers.Alt);
                        _enumeration.Add(Modifiers.Win);
                        break;
                    case 12:
                        HasShift = true;
                        HasWin = true;
                        _enumeration.Add(Modifiers.Shift);
                        _enumeration.Add(Modifiers.Win);
                        break;
                    case 13:
                        HasShift = true;
                        HasAlt = true;
                        HasWin = true;
                        _enumeration.Add(Modifiers.Shift);
                        _enumeration.Add(Modifiers.Alt);
                        _enumeration.Add(Modifiers.Win);
                        break;
                    case 14:
                        HasShift = true;
                        HasControl = true;
                        HasWin = true;
                        _enumeration.Add(Modifiers.Shift);
                        _enumeration.Add(Modifiers.Control);
                        _enumeration.Add(Modifiers.Win);
                        break;
                    case 15:
                        HasShift = true;
                        HasControl = true;
                        HasAlt = true;
                        HasWin = true;
                        _enumeration.Add(Modifiers.Shift);
                        _enumeration.Add(Modifiers.Control);
                        _enumeration.Add(Modifiers.Alt);
                        _enumeration.Add(Modifiers.Win);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(modifier),
                            "Modifier");
                }
            }

            /// <summary>Initializes this class.
            /// </summary>
            /// <param name="mod">the modifier to parse.</param>
            public ParseModifier(Modifiers mod) : this((int)mod) { }

            IEnumerator IEnumerable.GetEnumerator() {
                return _enumeration.GetEnumerator();
            }
        }
    }
}