using DevExpress.EasyTest.Framework;

namespace Xpand.TestsLib.EasyTest.Commands.DialogCommands{
    public class HandleDialogCommand:EasyTestCommand{
        public HandleDialogCommand(){
        }

        public HandleDialogCommand(string respond){
            Parameters.Add(new Parameter("Respond",respond));
        }

        protected override void ExecuteCore(ICommandAdapter adapter){
            var handleDialogCommand = this.ConnvertTo<DevExpress.EasyTest.Framework.Commands.HandleDialogCommand>();
            adapter.Execute(handleDialogCommand);
        }
    }
}