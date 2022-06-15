using System.Threading;
using DevExpress.EasyTest.Framework;

using Xpand.TestsLib.Common.Win32;

namespace Xpand.TestsLib.EasyTest.Commands.Automation{
    public class MoveWindowCommand:EasyTestCommand{
        private readonly int _x;
        private readonly int _y;
        private readonly int _width;
        private readonly int _height;
        private readonly bool _repaint;

        
        
        public MoveWindowCommand():this(0,0,1024,768){
        }

        public MoveWindowCommand(int x, int y, int width, int height, bool repaint=true){
            _x = x;
            _y = y;
            _width = width;
            _height = height;
            _repaint = repaint;
        }

        protected override void ExecuteCore(ICommandAdapter adapter){
            Win32Declares.Window.MoveWindow(Win32Declares.WindowFocus.GetForegroundWindow(), _x, _y, _width, _height, _repaint);
            Thread.Sleep(500);
        }
    }
}