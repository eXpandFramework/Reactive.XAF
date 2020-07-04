using DevExpress.EasyTest.Framework;

namespace Xpand.TestsLib.EasyTest.Commands.ActionCommands{
	public class ActionCommand:EasyTestCommand{
        public ActionCommand(string caption){
            Parameters.MainParameter=new MainParameter(caption);
            Parameters.ExtraParameter=new MainParameter();
        }

        protected override void ExecuteCore(ICommandAdapter adapter){
	        var actionCommand = new DevExpress.EasyTest.Framework.Commands.ActionCommand{
		        Parameters = {MainParameter = Parameters.MainParameter, ExtraParameter = new MainParameter()}
	        };
	        actionCommand.Execute(adapter);
        }
    }
}