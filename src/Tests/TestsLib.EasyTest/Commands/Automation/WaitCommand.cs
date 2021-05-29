using System;
using System.Threading;
using DevExpress.EasyTest.Framework;

namespace Xpand.TestsLib.EasyTest.Commands.Automation{
    public class WaitCommand:EasyTestCommand{
        public const string Name = "Wait";

        public WaitCommand(int millisecond=500){
            Parameters.MainParameter=new MainParameter(millisecond.ToString());
        }

        protected override void ExecuteCore(ICommandAdapter adapter){
            Thread.Sleep(Convert.ToInt32(Parameters.MainParameter.Value));
        }
    }
}