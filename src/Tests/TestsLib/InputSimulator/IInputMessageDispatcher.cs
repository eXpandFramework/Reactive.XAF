using Xpand.TestsLib.Win32;

namespace Xpand.TestsLib.InputSimulator{
    internal interface IInputMessageDispatcher{
        void DispatchInput(Win32Types.INPUT[] inputs);
    }
}