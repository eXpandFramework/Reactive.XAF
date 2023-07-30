using DevExpress.ExpressApp;
using Fasterflect;

namespace Xpand.Extensions.XAF.ViewExtensions{
	public static partial class ViewExtensions{
		
		public static XafApplication Application(this CompositeView view) => (XafApplication) view.GetPropertyValue("Application");
	}
}