using DevExpress.ExpressApp;
using Xpand.XAF.Modules.Workflow;

namespace Xpand.XAF.Modules.Telegram.Tests.Common{
    public class TestTelegramModule:ModuleBase {
        public TestTelegramModule(){
            RequiredModuleTypes.Add(typeof(TelegramModule));
            RequiredModuleTypes.Add(typeof(WorkflowModule));
        }
    }
}