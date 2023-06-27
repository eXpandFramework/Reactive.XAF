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

        public static bool Is(this IModelObjectView view, ViewType viewType) 
            => view is IModelDetailView && viewType == DevExpress.ExpressApp.ViewType.DetailView ||
               view is IModelListView && viewType == DevExpress.ExpressApp.ViewType.ListView || viewType == DevExpress.ExpressApp.ViewType.Any;
    }
}