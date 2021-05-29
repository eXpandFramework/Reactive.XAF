using System.ComponentModel;
using System.Linq;
using DevExpress.EasyTest.Framework;
using EnumsNET;
using Xpand.Extensions.XAF.ObjectExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;

namespace Xpand.TestsLib.EasyTest.Commands.ActionCommands{
    public class ActionCommand:EasyTestCommand{
        private static Actions _action;

        public ActionCommand(Actions action):this(ActionValue(action)){
        }

        private static string ActionValue(Actions action) {
            _action = action;
            var defaultValue = action.GetAttributes()?.OfType<DefaultValueAttribute>().FirstOrDefault()?.Value;
            return (string) (defaultValue??action.ToString());
        }

        public ActionCommand(string caption,string item=null){
            Parameters.MainParameter=new MainParameter(caption.CompoundName());
            var value = item?.CompoundName();
            Parameters.ExtraParameter=new MainParameter(value);
        }

        protected override void ExecuteCore(ICommandAdapter adapter) {
            if (adapter.GetTestApplication().Platform() == Platform.Blazor&&_action==Actions.SaveAndClose) {
                adapter.Execute(new ActionCommand(Actions.Save));
                adapter.Execute(new ActionCommand(Actions.Close));
            }
            else {
                var actionCommand = this.ConvertTo<DevExpress.EasyTest.Framework.Commands.ActionCommand>();
                adapter.Execute(actionCommand);
            }
        }
    }
}