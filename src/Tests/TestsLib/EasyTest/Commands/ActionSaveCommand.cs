using DevExpress.EasyTest.Framework;
using Xpand.TestsLib.EasyTest.Commands.ActionCommands;

namespace Xpand.TestsLib.EasyTest.Commands{
    public enum SaveCommandType{
        Close,
        New
    }
    public class ActionSaveCommand:EasyTestCommand{
        private readonly string _action;

        public ActionSaveCommand(SaveCommandType? type=null){
            _action = type==null ? "Save" : $"Save and {type}";
        }

        protected override void ExecuteCore(ICommandAdapter adapter){
            adapter.Execute(new ActionCommand(_action));
        }
    }
}