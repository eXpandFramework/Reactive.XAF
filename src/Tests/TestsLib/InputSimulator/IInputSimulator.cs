using Xpand.EasyTest.Automation.InputSimulator;

namespace Xpand.TestsLib.InputSimulator{
    public interface IInputSimulator{
        IKeyboardSimulator Keyboard { get; }
        IMouseSimulator Mouse { get; }
        IInputDeviceStateAdaptor InputDeviceState { get; }
    }
}