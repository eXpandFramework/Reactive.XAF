using System;
using System.Linq;
using System.Reactive.Linq;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Security.ClientServer;
using DevExpress.ExpressApp.Xpo;
using Moq;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;
using Xpand.XAF.Modules.Reactive.Tests.Common;

namespace Xpand.XAF.Modules.Reactive.Tests.ObjectSpaceProvider{
    public class ObjectSpaceProviderTests:ReactiveCommonTest{
        [Test]
        [XpandTest()]
        public void WhenSchemaUpdating(){
            using var application = DefaultReactiveModule().Application;
            using var testObserver = application.ObjectSpaceProvider.WhenSchemaUpdating().Test();

            application.ObjectSpaceProvider.UpdateSchema();

            testObserver.ItemCount.ShouldBe(1);
        }

        [Test]
        [XpandTest()]
        public void WhenSchemaUpdated(){
            using var application = DefaultReactiveModule().Application;
            using var testObserver = application.ObjectSpaceProvider.WhenSchemaUpdated().Test();

            application.ObjectSpaceProvider.UpdateSchema();

            testObserver.ItemCount.ShouldBe(1);
        }

        [TestCase(true,2)]
        [TestCase(false,1)]
        [XpandTest()]
        public void WhenObjectSpaceCreated(bool emitUpdatingOs,int expected){
            using var application = DefaultReactiveModule().Application;
            using var testObserver = application.ObjectSpaceProvider.WhenObjectSpaceCreated(emitUpdatingObjectSpace:emitUpdatingOs).Test();
            
            application.ObjectSpaceProvider.CreateObjectSpace();
            application.CreateNonSecuredObjectSpace(typeof(R));
            
            testObserver.ItemCount.ShouldBe(expected);
        }
        [Test]
        [XpandTest()][Order(-10)]
        public void When_Secured_ObjectSpaceCreated(){
            using var application = NewXafApplication();
            application.SetupSecurity();
            DefaultReactiveModule(application);
            var securedObjectSpaceProvider = new SecuredObjectSpaceProvider((ISelectDataSecurityProvider)application.Security, new ConnectionStringDataStoreProvider("XpoProvider=InMemoryDataStoreProvider"));
            using var testObserver = securedObjectSpaceProvider.WhenObjectSpaceCreated().Take(1).Test();

            var objectSpace = securedObjectSpaceProvider.CreateObjectSpace();

            testObserver.AwaitDone(Timeout).Items.Any(space => space==objectSpace).ShouldBeTrue();
        }
        [Test]
        [XpandTest()][Order(-10)]
        public void When_NonSecured_ObjectSpaceCreated(){
            using var application = NewXafApplication();
            application.SetupSecurity();
            var securedObjectSpaceProvider = new SecuredObjectSpaceProvider((ISelectDataSecurityProvider)application.Security, new ConnectionStringDataStoreProvider("XpoProvider=InMemoryDataStoreProvider"));
            application.AddObjectSpaceProvider(securedObjectSpaceProvider);
            DefaultReactiveModule(application);
            
            
            using var testObserver = securedObjectSpaceProvider.WhenObjectSpaceCreated().Take(1).Test();

            var nonsecuredObjectSpace = securedObjectSpaceProvider.CreateNonsecuredObjectSpace();

            testObserver.AwaitDone(Timeout).Items.Any(space => space==nonsecuredObjectSpace).ShouldBeTrue();
        }

        [Test]
        [XpandTest()]
        public void When_Secured_MiddleTier() {
            var objectSpaceProvider = new MiddleTierServerObjectSpaceProvider(Mock.Of<IMiddleTierSerializableObjectLayer>());

            objectSpaceProvider.PatchSchemaUpdated();

            Should.Throw<NotSupportedException>(() => objectSpaceProvider.UpdateSchema());        }
    }
}