using DevExpress.EasyTest.Framework;
using Xpand.TestsLib.EasyTest.Commands.ActionCommands;
using Xpand.TestsLib.EasyTest.Commands.DialogCommands;

namespace Xpand.TestsLib.EasyTest.Commands{
    public class ActionDeleteObjectsCommand:EasyTestCommand{

        protected override void ExecuteCore(ICommandAdapter adapter){
            adapter.Execute(new ActionCommand("Delete"));
            adapter.Execute(new RespondDialogCommand("OK"));
        }
    }
}