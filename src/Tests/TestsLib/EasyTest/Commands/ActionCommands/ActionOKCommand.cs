using DevExpress.EasyTest.Framework;

namespace Xpand.TestsLib.EasyTest.Commands.ActionCommands{
    public class ActionOKCommand:EasyTestCommand{
        public const string Name = "ActionOK";

        protected override void ExecuteCore(ICommandAdapter adapter){
            var command = new ActionCommand("OK");
            command.Execute(adapter);
        }
    }
}