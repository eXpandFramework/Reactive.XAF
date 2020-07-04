using System;
using System.Collections.Generic;
using Xpand.EasyTest.Automation.InputSimulator;
using Xpand.TestsLib.Win32;

namespace Xpand.TestsLib.InputSimulator{
    public interface IKeyboardSimulator{
        IMouseSimulator Mouse { get; }
        IKeyboardSimulator KeyDown(Win32Constants.VirtualKeys keyCode);
        IKeyboardSimulator KeyPress(Win32Constants.VirtualKeys keyCode);
        IKeyboardSimulator KeyPress(params Win32Constants.VirtualKeys[] keyCodes);
        IKeyboardSimulator KeyUp(Win32Constants.VirtualKeys keyCode);

        IKeyboardSimulator ModifiedKeyStroke(IEnumerable<Win32Constants.VirtualKeys> modifierKeyCodes,
            IEnumerable<Win32Constants.VirtualKeys> keyCodes);

        IKeyboardSimulator ModifiedKeyStroke(IEnumerable<Win32Constants.VirtualKeys> modifierKeyCodes,
            Win32Constants.VirtualKeys keyCode);

        IKeyboardSimulator ModifiedKeyStroke(Win32Constants.VirtualKeys modifierKey,
            IEnumerable<Win32Constants.VirtualKeys> keyCodes);

        IKeyboardSimulator ModifiedKeyStroke(Win32Constants.VirtualKeys modifierKeyCode,
            Win32Constants.VirtualKeys keyCode);

        IKeyboardSimulator TextEntry(string text);
        IKeyboardSimulator TextEntry(char character);
        IKeyboardSimulator Sleep(int millsecondsTimeout);
        IKeyboardSimulator Sleep(TimeSpan timeout);
    }
}