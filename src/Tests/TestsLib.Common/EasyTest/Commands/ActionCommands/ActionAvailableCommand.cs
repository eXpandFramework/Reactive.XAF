using DevExpress.EasyTest.Framework;

namespace Xpand.TestsLib.Common.EasyTest.Commands.ActionCommands{
	public class ActionAvailableCommand:EasyTestCommand{

        public ActionAvailableCommand(string caption){
            Parameters.MainParameter=new MainParameter(caption);
		}

		protected override void ExecuteCore(ICommandAdapter adapter){
			var actionAvailableCommand = this.ConvertTo<DevExpress.EasyTest.Framework.Commands.ActionAvailableCommand>();
            adapter.Execute(actionAvailableCommand);
		}
	}
}