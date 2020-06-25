using DevExpress.ExpressApp;
using Fasterflect;

namespace Xpand.Extensions.XAF.ViewExtenions{
	public static partial class ViewExtenions{
		public static XafApplication Application(this CompositeView view) => (XafApplication) view.GetPropertyValue("Application");
	}
}