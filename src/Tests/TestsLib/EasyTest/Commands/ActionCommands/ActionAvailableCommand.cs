using DevExpress.EasyTest.Framework;

namespace Xpand.TestsLib.EasyTest.Commands.ActionCommands{
	public class ActionAvailableCommand:EasyTestCommand{

        public ActionAvailableCommand(string caption){
            Parameters.MainParameter=new MainParameter(caption);
		}

		protected override void ExecuteCore(ICommandAdapter adapter){
			var actionAvailableCommand = this.ConnvertTo<DevExpress.EasyTest.Framework.Commands.ActionAvailableCommand>();
            adapter.Execute(actionAvailableCommand);
		}
	}
}