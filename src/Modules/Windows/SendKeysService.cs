using System;
using System.Windows.Forms;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.ObjectExtensions;

namespace Xpand.XAF.Modules.Windows{
    public static class SendKeysService{
        public static void Send(this Keys key, int count = 1, bool ctrl = false, bool alt = false, bool shift = false){
            var keyToSend = key.GetSendKeysRepresentation(ctrl, alt, shift);
            if (!string.IsNullOrEmpty(keyToSend)){
                0.Range(count).Do(_ => SendKeys.SendWait(keyToSend)).Enumerate();
            }
            else{
                throw new NotSupportedException($"The key {key} is not supported.");
            }
        }

        private static string GetSendKeysRepresentation(this Keys key, bool ctrl, bool alt, bool shift) {
            string keyRepresentation;

            switch (key) {
                case Keys.Back:
                    keyRepresentation = "{BACKSPACE}";
                    break;
                case Keys.Tab:
                    keyRepresentation = "{TAB}";
                    break;
                case Keys.Clear:
                    keyRepresentation = "{CLEAR}";
                    break;
                case Keys.Enter:
                    keyRepresentation = "{ENTER}";
                    break;
                case Keys.Pause:
                    keyRepresentation = "{PAUSE}";
                    break;
                case Keys.Escape:
                    keyRepresentation = "{ESC}";
                    break;
                case Keys.Space:
                    keyRepresentation = " ";
                    break;
                case Keys.PageUp:
                    keyRepresentation = "{PGUP}";
                    break;
                case Keys.PageDown:
                    keyRepresentation = "{PGDN}";
                    break;
                case Keys.End:
                    keyRepresentation = "{END}";
                    break;
                case Keys.Home:
                    keyRepresentation = "{HOME}";
                    break;
                case Keys.Left:
                    keyRepresentation = "{LEFT}";
                    break;
                case Keys.Up:
                    keyRepresentation = "{UP}";
                    break;
                case Keys.Right:
                    keyRepresentation = "{RIGHT}";
                    break;
                case Keys.Down:
                    keyRepresentation = "{DOWN}";
                    break;
                case Keys.Select:
                    keyRepresentation = "{SELECT}";
                    break;
                case Keys.Print:
                    keyRepresentation = "{PRINT}";
                    break;
                case Keys.Execute:
                    keyRepresentation = "{EXECUTE}";
                    break;
                case Keys.PrintScreen:
                    keyRepresentation = "{PRTSC}";
                    break;
                case Keys.Insert:
                    keyRepresentation = "{INSERT}";
                    break;
                case Keys.Delete:
                    keyRepresentation = "{DELETE}";
                    break;
                case Keys.Help:
                    keyRepresentation = "{HELP}";
                    break;
                case Keys.D0:
                case Keys.NumPad0:
                    keyRepresentation = "0";
                    break;
                case Keys.D1:
                case Keys.NumPad1:
                    keyRepresentation = "1";
                    break;
                case Keys.D2:
                case Keys.NumPad2:
                    keyRepresentation = "2";
                    break;
                case Keys.D3:
                case Keys.NumPad3:
                    keyRepresentation = "3";
                    break;
                case Keys.D4:
                case Keys.NumPad4:
                    keyRepresentation = "4";
                    break;
                case Keys.D5:
                case Keys.NumPad5:
                    keyRepresentation = "5";
                    break;
                case Keys.D6:
                case Keys.NumPad6:
                    keyRepresentation = "6";
                    break;
                case Keys.D7:
                case Keys.NumPad7:
                    keyRepresentation = "7";
                    break;
                case Keys.D8:
                case Keys.NumPad8:
                    keyRepresentation = "8";
                    break;
                case Keys.D9:
                case Keys.NumPad9:
                    keyRepresentation = "9";
                    break;
                case Keys.Multiply:
                    keyRepresentation = "*";
                    break;
                case Keys.Add:
                    keyRepresentation = "+";
                    break;
                case Keys.Separator:
                    keyRepresentation = "{SEPARATOR}";
                    break;
                case Keys.Subtract:
                    keyRepresentation = "-";
                    break;
                case Keys.Decimal:
                    keyRepresentation = ".";
                    break;
                case Keys.Divide:
                    keyRepresentation = "/";
                    break;
                case Keys.NumLock:
                    keyRepresentation = "{NUMLOCK}";
                    break;
                case Keys.Scroll:
                    keyRepresentation = "{SCROLLLOCK}";
                    break;
                case Keys.CapsLock:
                    keyRepresentation = "{CAPSLOCK}";
                    break;
                case Keys.LWin:
                case Keys.RWin:
                    keyRepresentation = "{WINDOWS}";
                    break;
                case Keys.Apps:
                    keyRepresentation = "{APPS}";
                    break;
                case Keys.Sleep:
                    keyRepresentation = "{SLEEP}";
                    break;
                case Keys.Zoom:
                    keyRepresentation = "{ZOOM}";
                    break;
                default:
                    keyRepresentation = HandleLetters(key);
                    break;
            }

            if (keyRepresentation == null)
                return null;
            
            keyRepresentation = HandleSpecialCharacters(keyRepresentation);
            
            keyRepresentation = AddModifierKeys(ctrl, alt, shift, keyRepresentation);
            return keyRepresentation;
        }

        private static string HandleLetters(Keys key){
            string keyRepresentation;
            if (key is >= Keys.A and <= Keys.Z) {
                keyRepresentation = key.ToString();
            }
            else if (key is >= Keys.F1 and <= Keys.F24) {
                keyRepresentation = HandleFunctionKeys(key);
            }
            else {
                keyRepresentation = null;
            }

            return keyRepresentation;
        }

        private static string HandleFunctionKeys(Keys key){
            int functionKeyNumber = key - Keys.F1 + 1;
            var keyRepresentation = $"{{F{functionKeyNumber}}}";
            return keyRepresentation;
        }

        private static string HandleSpecialCharacters(string keyRepresentation){
            if (keyRepresentation.Length == 1 && "^+%~(){}".Contains(keyRepresentation)) {
                keyRepresentation = "{" + keyRepresentation + "}";
            }
            return keyRepresentation;
        }

        private static string AddModifierKeys(bool ctrl, bool alt, bool shift, string keyRepresentation){
            if (ctrl) keyRepresentation = $"^({keyRepresentation})";
            if (alt) keyRepresentation = $"%({keyRepresentation})";
            if (shift) keyRepresentation = $"+({keyRepresentation})";
            return keyRepresentation;
        }
    }
}
