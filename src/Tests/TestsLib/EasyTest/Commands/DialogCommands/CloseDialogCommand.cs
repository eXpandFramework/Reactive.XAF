namespace Xpand.TestsLib.EasyTest.Commands.DialogCommands{
    public class CloseDialogCommand:HandleDialogCommand{
        public CloseDialogCommand(){
            Parameters.Add(new Parameter("Close","True"));
            Parameters.Add(new Parameter("Respond","OK"));
        }
    }
}