using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using Fasterflect;

namespace Xpand.Extensions.XAF.XafApplicationExtensions{
	public static partial class XafApplicationExtensions{
		public static ControllersManager ControllersManager(this XafApplication application) =>
			(ControllersManager) application.GetPropertyValue("ControllersManager");
	}
}