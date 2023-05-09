using DevExpress.ExpressApp;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;

namespace Xpand.XAF.Modules.Reactive.Tests.Common{
    public abstract class ReactiveCommonTest:BaseTest{
        protected virtual ReactiveModule DefaultReactiveModule(XafApplication application){
            application.AddModule<TestModule>(typeof(R),typeof(NonPersistentObject));
            return application.Modules.FindModule<ReactiveModule>();
        }
        protected virtual ReactiveModule DefaultSecuredReactiveModule(XafApplication application){
            application.SetupSecurity();
            application.AddSecuredProviderModule<TestModule>(typeof(R),typeof(NonPersistentObject));
            return application.Modules.FindModule<ReactiveModule>();
        }

        protected ReactiveModule DefaultReactiveModule(Platform platform=Platform.Win){
            var application = NewXafApplication(platform);
            return DefaultReactiveModule(application);
        }
        protected ReactiveModule DefaultSecuredReactiveModule(Platform platform=Platform.Win){
            var application = NewXafApplication(platform);
            return DefaultReactiveModule(application);
        }

        protected XafApplication NewXafApplication(Platform platform=Platform.Win) => platform.NewApplication<ReactiveModule>();
    }
}