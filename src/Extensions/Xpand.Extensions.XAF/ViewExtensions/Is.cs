using System;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ViewExtensions {
    public static partial class ViewExtensions {
        
        public static bool Is(this View view, ViewType viewType = ViewType.Any, Nesting nesting = Nesting.Any,
            Type objectType = null){
            objectType ??= typeof(object);
            return FitsCore(view, viewType) && FitsCore(view, nesting) && objectType.IsAssignableFrom(view.ObjectTypeInfo?.Type);
        }

        private static bool FitsCore(View view, ViewType viewType) 
            => view != null && (viewType == ViewType.ListView
                ? view is ListView : viewType == ViewType.DetailView
                    ? view is DetailView : viewType != ViewType.DashboardView || view is DashboardView);

        private static bool FitsCore(View view, Nesting nesting) 
            => nesting == Nesting.Nested ? !view.IsRoot : nesting != Nesting.Root || view.IsRoot;
    }
}