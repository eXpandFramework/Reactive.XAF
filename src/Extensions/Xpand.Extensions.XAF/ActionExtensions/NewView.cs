using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Model;
using Xpand.Extensions.XAF.XafApplicationExtensions;

namespace Xpand.Extensions.XAF.ActionExtensions {
    public static partial class ActionExtensions {
        public static DetailView NewDetailView(this ActionBaseEventArgs e, Type type,
            TargetWindow targetWindow = TargetWindow.Default, bool isRoot = false)
            => e.NewDetailView(e.Application().FindDetailViewId(type), targetWindow, isRoot);
        public static View NewDetailView(this ActionBaseEventArgs e, object currentObject,
            TargetWindow targetWindow = TargetWindow.Default, bool isRoot = false)
            => e.NewDetailView(e.Application().FindDetailViewId(currentObject.GetType()),currentObject, targetWindow, isRoot);

        public static DetailView NewDetailView(this ActionBaseEventArgs e,string viewId,TargetWindow targetWindow=TargetWindow.Default,bool isRoot=false){
            var detailView = e.ShowViewParameters.CreatedView = e.Application().NewDetailView(
                e.Action.View().SelectedObjects.Cast<object>().First(),
                (IModelDetailView)e.Application().FindModelView(viewId),isRoot);
            e.ShowViewParameters.TargetWindow = targetWindow;
            return (DetailView)detailView;
        }
        public static DetailView NewDetailView(this ActionBaseEventArgs e,string viewId,object current,TargetWindow targetWindow=TargetWindow.Default,bool isRoot=false){
            var detailView = e.ShowViewParameters.CreatedView = e.Application().NewDetailView(current,
                (IModelDetailView)e.Application().FindModelView(viewId),isRoot);
            e.ShowViewParameters.TargetWindow = targetWindow;
            return (DetailView)detailView;
        }
    }
}