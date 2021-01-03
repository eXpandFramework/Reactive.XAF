using DevExpress.EasyTest.Framework;
using Fasterflect;

namespace Xpand.TestsLib.Common.EasyTest.Commands.Automation{
    public class SetHtmlElementValueCommand:EasyTestCommand{
        public SetHtmlElementValueCommand(string identifier,bool byName=false){
        }

        protected override void ExecuteCore(ICommandAdapter adapter){
            if (adapter.CallMethod("ExecuteScript") is string errorMessage)
                throw new AdapterOperationException(errorMessage);
            adapter.CallMethod("WaitForBrowserResponse");
        }
    }
}