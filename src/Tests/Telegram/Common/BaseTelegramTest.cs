using DevExpress.ExpressApp;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.Telegram.Tests.Common {
	public abstract class BaseTelegramTest:BaseTest {
		protected XafApplication NewApplication() => Platform.Win.NewApplication<ReactiveModule>(mockEditors:false);
		protected virtual TelegramModule TelegramModule(XafApplication application){
			application.AddModule<TestTelegramModule>();
			return application.Modules.FindModule<TelegramModule>();
		}
	}

}