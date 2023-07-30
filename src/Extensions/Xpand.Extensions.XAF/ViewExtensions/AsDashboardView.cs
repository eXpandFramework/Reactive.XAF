using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ViewExtensions{
    public static partial class ViewExtensions{
        public static DashboardView AsDashboardView(this View view) => view as DashboardView;
        public static DashboardView ToDashboardView(this View view) => (DashboardView)view ;
        
        public static CompositeView ToCompositeView(this View view) => (CompositeView)view ;
    }
}