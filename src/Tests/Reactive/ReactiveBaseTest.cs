using DevExpress.ExpressApp;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.TestsLib;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;

namespace Xpand.XAF.Modules.Reactive.Tests{
    public abstract class ReactiveBaseTest:BaseTest{
        protected ReactiveModule DefaultReactiveModule(XafApplication application){
            application.AddModule<TestModule>(typeof(R),typeof(NonPersistentObject));
            return application.Modules.FindModule<ReactiveModule>();
        }

        protected ReactiveModule DefaultReactiveModule(Platform platform=Platform.Win){
            var application = platform.NewApplication<ReactiveModule>();
            return DefaultReactiveModule(application);
        }

    }
}