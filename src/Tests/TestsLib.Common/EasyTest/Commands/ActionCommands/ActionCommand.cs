using System.ComponentModel;
using System.Linq;
using DevExpress.EasyTest.Framework;
using EnumsNET;
using Xpand.Extensions.XAF.ObjectExtensions;

namespace Xpand.TestsLib.Common.EasyTest.Commands.ActionCommands{
    public class ActionCommand:EasyTestCommand{
        public ActionCommand(Actions action):this(ActionValue(action)){
            
        }
        private static string ActionValue(Actions action){
            var defaultValue = action.GetAttributes()?.OfType<DefaultValueAttribute>().FirstOrDefault()?.Value;
            return (string) (defaultValue??action.ToString());
        }

        public ActionCommand(string caption,string item=null){
            Parameters.MainParameter=new MainParameter(caption.CompoundName());
            Parameters.ExtraParameter=new MainParameter(item);
        }

        protected override void ExecuteCore(ICommandAdapter adapter) {
            var actionCommand = this.ConvertTo<DevExpress.EasyTest.Framework.Commands.ActionCommand>();
            adapter.Execute(actionCommand);
        }
    }
}