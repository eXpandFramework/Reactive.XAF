using DevExpress.EasyTest.Framework;

namespace Xpand.TestsLib.EasyTest.Commands.ActionCommands{
    public class ActionCommand:DevExpress.EasyTest.Framework.Commands.ActionCommand{
        public ActionCommand(string caption){
            Parameters.MainParameter=new MainParameter(caption);
            Parameters.ExtraParameter=new MainParameter();
        }

    }
}