using Xpand.TestsLib.Common;

namespace Xpand.XAF.Modules.Reactive.Logger.Tests{
    public class TestReactiveLoggerModule:EmptyModule{
        public TestReactiveLoggerModule(){
            RequiredModuleTypes.Add(typeof(ReactiveLoggerModule));
        }
    }
}