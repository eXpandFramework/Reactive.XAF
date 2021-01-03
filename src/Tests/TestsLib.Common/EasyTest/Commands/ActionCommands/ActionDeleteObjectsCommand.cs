using DevExpress.EasyTest.Framework;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common.EasyTest.Commands.DialogCommands;

namespace Xpand.TestsLib.Common.EasyTest.Commands.ActionCommands{
    public class ActionDeleteObjectsCommand:EasyTestCommand{

        protected override void ExecuteCore(ICommandAdapter adapter){
            adapter.Execute(new ActionCommand("Delete"){SuppressExceptions = true});
            adapter.Execute(new RespondDialogCommand(adapter.GetTestApplication().Platform()==Platform.Win?"Yes":"OK"){SuppressExceptions = true});
        }
    }
}