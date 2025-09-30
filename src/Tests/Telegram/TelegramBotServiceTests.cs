using System;
using NUnit.Framework;
using Xpand.XAF.Modules.Telegram.BusinessObjects;
using Xpand.XAF.Modules.Telegram.Services;

namespace Xpand.XAF.Modules.Telegram.Tests{
    [TestFixture]
    public partial class TelegramBotServiceTests : TelegramIntegrationTestBase {
        private Func<TelegramBot, IObservable<TelegramBotClient>> _originalClientFactory;

        [SetUp]
        public override void Setup() {
            _originalClientFactory = TelegramBotService.ClientFactory;
            TelegramBotService.Clients.Clear();
            base.Setup();
        }

        protected override TestXafApplication NewTestXafApplication() {
            var xafApplication = base.NewTestXafApplication();
            xafApplication.Modules.Add(new TelegramModule());
            return xafApplication;
        }

        [TearDown]
        public override void TearDown() {
            TelegramBotService.ClientFactory = _originalClientFactory;
            TelegramBotService.Clients.Clear();
            base.TearDown();
        }


    }
}