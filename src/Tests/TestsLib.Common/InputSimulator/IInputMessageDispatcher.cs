using Xpand.TestsLib.Common.Win32;

namespace Xpand.TestsLib.Common.InputSimulator{
    internal interface IInputMessageDispatcher{
        void DispatchInput(Win32Types.INPUT[] inputs);
    }
}