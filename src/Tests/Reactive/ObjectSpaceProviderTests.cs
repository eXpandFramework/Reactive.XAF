using System;
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

namespace Xpand.XAF.Modules.Reactive.Tests{
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

        [Test]
        [XpandTest()][Ignore("fail when run all")]
        public void WhenObjectSpaceCreated(){
            using var application = DefaultReactiveModule().Application;
            using var testObserver = application.ObjectSpaceProvider.WhenObjectSpaceCreated().Test();
            
            application.ObjectSpaceProvider.CreateObjectSpace();
            application.CreateNonSecuredObjectSpace(typeof(R));
            
            testObserver.ItemCount.ShouldBe(1);
        }
        [Test]
        [XpandTest()][Order(-10)]
        public void When_Secured_ObjectSpaceCreated(){
            using var application = NewXafApplication();
            application.SetupSecurity();
            DefaultReactiveModule(application);
            var securedObjectSpaceProvider = new SecuredObjectSpaceProvider((ISelectDataSecurityProvider)application.Security, new ConnectionStringDataStoreProvider("XpoProvider=InMemoryDataStoreProvider"));
            using var testObserver = securedObjectSpaceProvider.WhenObjectSpaceCreated().Test();

            securedObjectSpaceProvider.CreateObjectSpace();

            testObserver.ItemCount.ShouldBe(1);
        }
        [Test]
        [XpandTest()][Order(-10)]
        public void When_NonSecured_ObjectSpaceCreated(){
            using var application = NewXafApplication();
            application.SetupSecurity();
            DefaultReactiveModule(application);
            var securedObjectSpaceProvider = new SecuredObjectSpaceProvider((ISelectDataSecurityProvider)application.Security, new ConnectionStringDataStoreProvider("XpoProvider=InMemoryDataStoreProvider"));
            using var testObserver = securedObjectSpaceProvider.WhenObjectSpaceCreated().Test();

            securedObjectSpaceProvider.CreateNonsecuredObjectSpace();

            testObserver.ItemCount.ShouldBe(1);
        }

        [Test]
        [XpandTest()]
        public void When_Secured_MiddleTier() {
            var objectSpaceProvider = new MiddleTierServerObjectSpaceProvider(Mock.Of<IMiddleTierSerializableObjectLayer>());

            objectSpaceProvider.PatchSchemaUpdated();

            Should.Throw<NotSupportedException>(() => objectSpaceProvider.UpdateSchema());        }
    }
}