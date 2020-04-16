using DevExpress.EasyTest.Framework;

namespace Xpand.TestsLib.EasyTest.Commands.ActionCommands{
    public class ActionCancelCommand:EasyTestCommand{
        public const string Name = "ActionCancel";

        protected override void ExecuteCore(ICommandAdapter adapter){
            var command = new ActionCommand("Cancel");
            command.Execute(adapter);
        }
    }
}