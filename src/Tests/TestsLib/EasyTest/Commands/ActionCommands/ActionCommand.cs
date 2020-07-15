using System.ComponentModel;
using System.Linq;
using DevExpress.EasyTest.Framework;
using EnumsNET;

namespace Xpand.TestsLib.EasyTest.Commands.ActionCommands{
    public class ActionCommand:EasyTestCommand{
        public ActionCommand(Actions action):this(ActionValue(action)){
            // Parameters.Add(new Parameter("action",action.ToString()));
        }
        private static string ActionValue(Actions action){
            var defaultValue = action.GetAttributes()?.OfType<DefaultValueAttribute>().FirstOrDefault()?.Value;
            return (string) (defaultValue??action.ToString());
        }

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