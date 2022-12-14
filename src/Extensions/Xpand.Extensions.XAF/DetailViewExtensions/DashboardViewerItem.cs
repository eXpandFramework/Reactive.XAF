using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;

namespace Xpand.Extensions.XAF.DetailViewExtensions {
    public static partial class DetailViewExtensions {
        public static IEnumerable<DashboardViewItem> DashboardViewerItem(this DetailView detailView)
            => detailView.GetItems<DashboardViewItem>().Where(item => item.Model.View.Id == "DashboardViewer_DetailView");
    }
}