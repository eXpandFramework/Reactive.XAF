using DevExpress.ExpressApp;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.CloneModelView.Tests {
    public class TestCloneModelViewModule:ModuleBase {
        public TestCloneModelViewModule(){
            RequiredModuleTypes.Add(typeof(ReactiveModule));
            RequiredModuleTypes.Add(typeof(CloneModelViewModule));
        }
    }
}