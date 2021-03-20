using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ViewExtensions{
    public static partial class ViewExtensions{
        public static DashboardView AsDashboardView(this View view) => view as DashboardView;
    }
}