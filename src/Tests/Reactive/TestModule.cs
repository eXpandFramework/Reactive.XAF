using DevExpress.ExpressApp;


namespace Xpand.XAF.Modules.Reactive.Tests{
    
    public class TestModule:ModuleBase{

	    public TestModule(){
            RequiredModuleTypes.Add(typeof(ReactiveModule));
        }

    }


}