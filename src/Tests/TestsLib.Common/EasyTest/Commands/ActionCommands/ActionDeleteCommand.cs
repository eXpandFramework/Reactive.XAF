using DevExpress.EasyTest.Framework;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common.EasyTest.Commands.DialogCommands;

namespace Xpand.TestsLib.Common.EasyTest.Commands.ActionCommands{
    public class ActionDeleteCommand:ActionCommand{
        private readonly bool _confirm;

        public ActionDeleteCommand(bool confirm=true):base("Delete"){
            _confirm = confirm;
        }

        protected override void ExecuteCore(ICommandAdapter adapter){
            base.ExecuteCore(adapter);
            if (_confirm){
                adapter.Execute(new HandleDialogCommand(adapter.GetTestApplication().Platform()!=Platform.Web?"Yes":"OK"));
            }
        }
    }
}