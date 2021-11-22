using DevExpress.EasyTest.Framework;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.XAF.ObjectExtensions;

namespace Xpand.TestsLib.EasyTest.Commands.ActionCommands{
	public class ActionAvailableCommand:EasyTestCommand{

        public ActionAvailableCommand(string caption){
            Parameters.MainParameter=new MainParameter(caption.CompoundName());
		}

		protected override void ExecuteCore(ICommandAdapter adapter){
			var actionAvailableCommand = this.ConvertTo<DevExpress.EasyTest.Framework.Commands.ActionAvailableCommand>();
            adapter.Execute(actionAvailableCommand);
		}
	}
}