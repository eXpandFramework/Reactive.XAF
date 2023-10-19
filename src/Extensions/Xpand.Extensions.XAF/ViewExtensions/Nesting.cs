using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ViewExtensions {
    public static partial class ViewExtensions {
        public static Nesting Nesting(this View view)
            => view.IsRoot ? DevExpress.ExpressApp.Nesting.Root : DevExpress.ExpressApp.Nesting.Nested;
    }
}