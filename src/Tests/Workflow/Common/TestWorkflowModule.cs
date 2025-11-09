using DevExpress.ExpressApp;

namespace Xpand.XAF.Modules.Workflow.Tests.Common{
    public class TestWorkflowModule:ModuleBase {
        public TestWorkflowModule(){
            RequiredModuleTypes.Add(typeof(WorkflowModule));
        }
    }
}