using DevExpress.EasyTest.Framework;

namespace Xpand.TestsLib.EasyTest.Commands.DialogCommands{
    public class HandleDialogCommand:EasyTestCommand{
        public HandleDialogCommand(){
        }

        public HandleDialogCommand(string respond){
            Parameters.Add(new Parameter("Respond",respond));
        }

        protected override void ExecuteCore(ICommandAdapter adapter){
            // if (adapter.GetTestApplication().Platform() == Platform.Blazor) {
            //     var dialog = new DialogTestControl(((CommandAdapter) adapter).Driver);
            //     dialog.Act(Parameters["Respond"].Value);
            // }
            // else {
                var handleDialogCommand = this.ConvertTo<DevExpress.EasyTest.Framework.Commands.HandleDialogCommand>();
                adapter.Execute(handleDialogCommand);
            // }
        }
    }
}