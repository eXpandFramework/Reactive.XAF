using System;
using System.Threading;
using DevExpress.EasyTest.Framework;

namespace Xpand.TestsLib.EasyTest.Commands{
    public class WaitCommand:EasyTestCommand{
        public const string Name = "Wait";

        public WaitCommand(int millisecs=500){
            Parameters.MainParameter=new MainParameter(millisecs.ToString());
        }

        protected override void ExecuteCore(ICommandAdapter adapter){
            Thread.Sleep(Convert.ToInt32(Parameters.MainParameter.Value));
        }
    }
}