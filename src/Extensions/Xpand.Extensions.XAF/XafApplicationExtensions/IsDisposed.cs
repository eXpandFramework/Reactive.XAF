using DevExpress.ExpressApp;
using Fasterflect;

namespace Xpand.Extensions.XAF.XafApplicationExtensions {
	public static partial class XafApplicationExtensions {
		static readonly MemberGetter IsDisposedGetter = typeof(XafApplication).DelegateForGetPropertyValue("IsDisposed");
		public static bool IsDisposed(this XafApplication application) 
			=> (bool)IsDisposedGetter(application);
	}
}