using DevExpress.ExpressApp;
using JetBrains.Annotations;

namespace Xpand.XAF.Modules.Reactive.Tests{
    [UsedImplicitly]
    public class TestModule:ModuleBase{

	    public TestModule(){
            RequiredModuleTypes.Add(typeof(ReactiveModule));
        }

    }


}