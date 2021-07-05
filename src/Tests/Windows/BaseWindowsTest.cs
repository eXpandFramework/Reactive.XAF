using DevExpress.ExpressApp.Editors;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Windows.Tests.BOModel;

namespace Xpand.XAF.Modules.Windows.Tests {
	public abstract class BaseWindowsTest:BaseTest {
		protected WindowsModule WindowsModule(Platform platform=Platform.Win){
			var application = platform.NewApplication<WindowsModule>();
			application.EditorFactory=new EditorsFactory();
			var oneViewModule = application.AddModule<WindowsModule>(typeof(W), typeof(W));
            
			return oneViewModule;
		}

	}
}