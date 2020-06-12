using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ViewExtenions{
    public static partial class ViewExtenions{
        public static DashboardView AsDashboardView(this View view) => view as DashboardView;
    }
}