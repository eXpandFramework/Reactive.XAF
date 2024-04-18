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

        public static DetailView NewDetailView(this ActionBaseEventArgs e,string viewId,TargetWindow targetWindow=TargetWindow.Default,bool isRoot=false) {
            var modelDetailView = (IModelDetailView)e.Application().FindModelView(viewId);
            var instance = e.Action.View().SelectedObjects.Cast<object>().First();
            var detailView = e.ShowViewParameters.CreatedView = 
                instance.GetType() == modelDetailView.ModelClass.TypeInfo.Type?e.Application().NewDetailView(instance, modelDetailView,isRoot):
                    e.Application().NewDetailView(modelDetailView.ModelClass.TypeInfo.Type);
            detailView.CurrentObject ??= detailView.ObjectSpace.CreateObject(modelDetailView.ModelClass.TypeInfo.Type);
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