using System.Text;
using System.Threading;
using DevExpress.EasyTest.Framework;
using Xpand.TestsLib.Common.Win32;

namespace Xpand.TestsLib.Common.EasyTest.Commands.Automation{
    public class WaitWindowFocusCommand:EasyTestCommand{
        private readonly string _windowTitle;

        public WaitWindowFocusCommand(string windowTitle){
            _windowTitle = windowTitle;
        }

        protected override void ExecuteCore(ICommandAdapter adapter){
            while (true ){
                var foregroundWindow = Win32Declares.WindowFocus.GetForegroundWindow();
                var stringBuilder = new StringBuilder(256);
                Win32Declares.Window.GetWindowText(foregroundWindow, stringBuilder, 256);
                if (stringBuilder.ToString() == _windowTitle){
                    break;
                }
            }
            Thread.Sleep(500);
        }
    }
}