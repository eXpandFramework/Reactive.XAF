using DevExpress.EasyTest.Framework;

namespace Xpand.TestsLib.EasyTest.Commands.ActionCommands{
    public class ActionOKCommand:EasyTestCommand{
	    private readonly bool _optional;
	    public const string Name = "ActionOK";

        public ActionOKCommand(bool optional=true){
	        _optional = optional;
        }

        protected override void ExecuteCore(ICommandAdapter adapter){
	        var command = new ActionCommand("OK"){SuppressExceptions = _optional};
	        command.Execute(adapter);
        }
    }
}