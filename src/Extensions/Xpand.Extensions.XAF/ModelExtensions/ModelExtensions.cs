using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.ModelExtensions {
    public partial class ModelExtensions {
        public static ViewType ViewType(this IModelView view)
            => view switch {
                IModelListView => DevExpress.ExpressApp.ViewType.ListView,
                IModelDetailView => DevExpress.ExpressApp.ViewType.DetailView,
                IModelDashboardView => DevExpress.ExpressApp.ViewType.DashboardView,
                _ => throw new NotImplementedException(view.GetType().FullName)
            };
    }
}