using System.Linq;
using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Security.ClientServer;
using DevExpress.ExpressApp.Xpo;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;
using Xpand.XAF.Modules.Reactive.Tests.Common;

namespace Xpand.XAF.Modules.Reactive.Tests.ObjectSpaceProvider{
    public class WhenSecuredProviderCreatedTests:ReactiveCommonTest {
        [Test]
        public void Application_ObjectSpaceCreated() {
            using var application = Platform.Win.NewApplication<ReactiveModule>();
            application.DatabaseUpdateMode=DatabaseUpdateMode.Never;
            DefaultSecuredReactiveModule(application);
            
            using var testObserver = application.WhenProviderObjectSpaceCreated().OfType<XPObjectSpace>().Test();
            
            var objectSpace = application.CreateObjectSpace();
            
            testObserver.ItemCount.ShouldBe(1);
            testObserver.Items.Last().ShouldBe(objectSpace);
        }
        [Test]
        public void Application_NonSecuredObjectSpace() {
            using var application = Platform.Win.NewApplication<ReactiveModule>();
            application.SetupSecurity();
            application.AddObjectSpaceProvider(new SecuredObjectSpaceProvider(
                (ISelectDataSecurityProvider)application.Security, new MemoryDataStoreProvider()));
            application.DatabaseUpdateMode=DatabaseUpdateMode.Never;
            DefaultReactiveModule(application);
            
            using var testObserver = application.WhenProviderObjectSpaceCreated(true).OfType<XPObjectSpace>().Test();
            
            var objectSpace = application.CreateNonSecuredObjectSpace(typeof(R));
            
            testObserver.Items.Last().ShouldBe(testObserver.Items.First());
            testObserver.ItemCount.ShouldBe(1);
            testObserver.Items.Last().ShouldBe(objectSpace);
        }

    }
}