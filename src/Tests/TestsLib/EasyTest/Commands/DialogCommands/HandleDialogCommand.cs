namespace Xpand.TestsLib.EasyTest.Commands.DialogCommands{
    public class HandleDialogCommand:DevExpress.EasyTest.Framework.Commands.HandleDialogCommand{
        public HandleDialogCommand(){
        }

        public HandleDialogCommand(string respond){
            Parameters.Add(new Parameter("Respond",respond));
        }
    }
}