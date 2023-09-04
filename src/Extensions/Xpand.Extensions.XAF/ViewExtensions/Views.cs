using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Layout;

namespace Xpand.Extensions.XAF.ViewExtensions {
    public static partial class ViewExtensions {
        public static IEnumerable<TControl> Controls<TControl>(CompositeView compositeView) 
            => compositeView.GetItems<ControlViewItem>().Select(item => item.Control)
                .OfType<TControl>();
        
        public static IEnumerable<View> Views(this DashboardView dashboardView)
            => dashboardView.Views<View>();
        
        public static IEnumerable<TView> Views<TView>(this DashboardView dashboardView) where TView:View
            => dashboardView.GetItems<DashboardViewItem>().Select(item => item.InnerView).OfType<TView>();
        
        public static IEnumerable<NestedFrame> NestedFrames<TView>(this DashboardView dashboardView,params Type[] objectTypes) where TView:View 
            => dashboardView.GetItems<DashboardViewItem>()
                .Where(item => item.InnerView is TView && (!objectTypes.Any()||objectTypes.Contains(item.InnerView.ObjectTypeInfo.Type)))
                .Select(item => item.Frame).Cast<NestedFrame>();
        
        public static IEnumerable<NestedFrame> NestedFrames(this DashboardView dashboardView,params ViewType[] viewTypes)
            => dashboardView.GetItems<DashboardViewItem>().Where(item => viewTypes.All(viewType =>item.InnerView.Is(viewType) )).Select(item => item.Frame).Cast<NestedFrame>();
        
        public static IEnumerable<DashboardViewItem> Items(this DashboardView dashboardView,params ViewType[] viewTypes)
            => dashboardView.GetItems<DashboardViewItem>().Where(item => viewTypes.Any(viewType =>item.InnerView.Is(viewType) ));
    }
}