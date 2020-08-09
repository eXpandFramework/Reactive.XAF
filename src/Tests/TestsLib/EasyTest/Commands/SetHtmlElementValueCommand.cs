using DevExpress.EasyTest.Framework;
using DevExpress.ExpressApp.EasyTest.WebAdapter;

namespace Xpand.TestsLib.EasyTest.Commands{
    public class SetHtmlElementValueCommand:EasyTestCommand{
        public SetHtmlElementValueCommand(string identifier,bool byName=false){
        }

        protected override void ExecuteCore(ICommandAdapter adapter){
            var webCommandAdapter = ((IWebCommandAdapter) adapter);
            if (webCommandAdapter.ExecuteScript("document.getElementById('i0116').value='sss'") is string errorMessage)
                throw new AdapterOperationException(errorMessage);
            webCommandAdapter.WaitForBrowserResponse();
        }
    }
}