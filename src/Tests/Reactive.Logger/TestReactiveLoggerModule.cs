using Xpand.TestsLib;

namespace Xpand.XAF.Modules.Reactive.Logger.Tests{
    public class TestReactiveLoggerModule:EmptyModule{
        public TestReactiveLoggerModule(){
            RequiredModuleTypes.Add(typeof(ReactiveLoggerModule));
        }
    }
}