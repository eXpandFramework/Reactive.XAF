using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Layout;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Xpo.DB;
using NUnit.Framework;

namespace Xpand.XAF.Modules.Telegram.Tests{
    public class TestXafApplication : XafApplication {
        protected override void CreateDefaultObjectSpaceProvider(CreateCustomObjectSpaceProviderEventArgs args) {
            // This override prevents the base class from throwing a NotImplementedException.
        }

        protected override LayoutManager CreateLayoutManagerCore(bool simple) => throw new System.NotImplementedException();
    }

    [TestFixture]
    public abstract class TelegramIntegrationTestBase {
        protected XafApplication Application { get; private set; }
        protected IObjectSpaceProvider ObjectSpaceProvider => Application.ObjectSpaceProvider;

        [OneTimeSetUp]
        public void OnTimeSetup() {
            XpoTypesInfoHelper.Reset();
            XpoTypesInfoHelper.ForceInitialize();
        }

        [SetUp]
        public virtual void Setup() {
            XpoTypesInfoHelper.Reset();
            XpoTypesInfoHelper.ForceInitialize();
            
            // var typesInfo = XpoTypesInfoHelper.GetTypesInfo();
            // typesInfo.RegisterEntity(typeof(TelegramBot));
            // typesInfo.RegisterEntity(typeof(TelegramChat));
            // typesInfo.RegisterEntity(typeof(TelegramUser));
            // typesInfo.RegisterEntity(typeof(TelegramChatMessage));
            // typesInfo.RegisterEntity(typeof(TelegramBotCommand));
            // typesInfo.RegisterEntity(typeof(TelegramCommandParameter));
            // typesInfo.RegisterEntity(typeof(TelegramMessageTemplate));

            // var dataStore = new InMemoryDataStore();
            // ObjectSpaceProvider = new XPObjectSpaceProvider(new ConnectionStringDataStoreProvider(dataStore.ConnectionString), typesInfo);
            
            Application = NewTestXafApplication();
            // Application.ObjectSpaceProvider = ObjectSpaceProvider;
            
            Application.Setup();
        }

        protected virtual TestXafApplication NewTestXafApplication() => new();

        [TearDown]
        public virtual void TearDown() {
            Application?.Dispose();
        }
    }
}