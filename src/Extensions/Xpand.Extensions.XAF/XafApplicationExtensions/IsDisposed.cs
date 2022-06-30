using DevExpress.ExpressApp;
using Fasterflect;

namespace Xpand.Extensions.XAF.XafApplicationExtensions;

public static partial class XafApplicationExtensions {
	public static bool IsDisposed(this XafApplication application) => (bool)application.GetPropertyValue("IsDisposed");
}