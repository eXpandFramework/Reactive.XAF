using System;
using DevExpress.ExpressApp;
using NUnit.Framework;
using Telegram.Bot;
using Xpand.Extensions.Reactive.Channels;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Telegram.BusinessObjects;
using Xpand.XAF.Modules.Telegram.Services;

namespace Xpand.XAF.Modules.Telegram.Tests.Common {
	public abstract class BaseTelegramTest:BaseTest {
		private Func<TelegramBot, IObservable<ITelegramBotClient>> _clientFactory;
		protected XafApplication NewApplication() => Platform.Win.NewApplication<ReactiveModule>(mockEditors:false);

		protected virtual TelegramModule TelegramModule(XafApplication application){
			application.AddModule<TestTelegramModule>();
			return application.Modules.FindModule<TelegramModule>();
		}

		[TearDown]
		public override void Dispose() {
			base.Dispose();
			TelegramBotService.ClientFactory = _clientFactory;
			RpcChannel.Reset();
			TelegramBotService.Clients.Clear();
		}

		[SetUp]
		public override void Setup() {
			base.Setup();
			_clientFactory = TelegramBotService.ClientFactory;
		}
	}

}