using DevExpress.EasyTest.Framework;

namespace Xpand.TestsLib.EasyTest.Commands.ActionCommands{
	public class ActionAvailableCommand:EasyTestCommand{
		private readonly string _caption;

		public ActionAvailableCommand(string caption){
			_caption = caption;
		}

		protected override void ExecuteCore(ICommandAdapter adapter){
			var actionAvailableCommand = new DevExpress.EasyTest.Framework.Commands.ActionAvailableCommand{
				Parameters = {MainParameter = new MainParameter(_caption), ExtraParameter = new MainParameter()}
			};
			actionAvailableCommand.Execute(adapter);
		}
	}
}