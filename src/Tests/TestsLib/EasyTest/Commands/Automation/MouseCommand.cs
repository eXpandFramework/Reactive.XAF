using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using DevExpress.EasyTest.Framework;
using Xpand.TestsLib.Common.InputSimulator;

namespace Xpand.TestsLib.EasyTest.Commands.Automation{
    
    public class MouseCommand:EasyTestCommand{
        private readonly Point _moveTo;
        private readonly Action<IMouseSimulator> _execute;
        private static readonly Common.InputSimulator.InputSimulator Simulator=new Common.InputSimulator.InputSimulator();

        public MouseCommand(Point moveTo,Action<IMouseSimulator> execute=null){
            _moveTo = moveTo;
            _execute = execute ?? (simulator => simulator.LeftButtonClick());
        }
        [DllImport("User32.dll")]
        public static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("User32.dll")]
        public static extern void ReleaseDC(IntPtr hwnd, IntPtr dc);
        
        void Blink(Point position){
            for (int i = 0; i < 7; i++){
            
                var desktopPtr = GetDC(IntPtr.Zero);
                Graphics g = Graphics.FromHdc(desktopPtr);

                SolidBrush b = new SolidBrush(Color.White);
                var rectangle = new Rectangle(position.X, position.Y, 10, 10);
                g.FillEllipse(b, rectangle);

                g.Dispose();
                ReleaseDC(IntPtr.Zero, desktopPtr);
                Thread.Sleep(50);
                
                desktopPtr = GetDC(IntPtr.Zero);
                g = Graphics.FromHdc(desktopPtr);

                b = new SolidBrush(Color.Black);
                g.FillEllipse(b, rectangle);

                g.Dispose();
                ReleaseDC(IntPtr.Zero, desktopPtr);
                Thread.Sleep(50);
            }
        }
        
        protected override void ExecuteCore(ICommandAdapter adapter){
            if (_moveTo!=default){
                Simulator.Mouse.MoveMouseTo(_moveTo.X, _moveTo.Y);
            }
            Blink(_moveTo);
            _execute(Simulator.Mouse);
            Thread.Sleep(500);
        }
    }
    
    
    
}