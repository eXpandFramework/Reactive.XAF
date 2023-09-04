using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Model;
using Xpand.Extensions.XAF.XafApplicationExtensions;

namespace Xpand.Extensions.XAF.ActionExtensions {
    public static partial class ActionExtensions {
        public static View NewDetailView(this ActionBaseEventArgs e,string viewId,TargetWindow targetWindow=TargetWindow.Default,bool isRoot=false){
            var detailView = e.ShowViewParameters.CreatedView = e.Application().NewDetailView(
                e.Action.View().SelectedObjects.Cast<object>().First(),
                (IModelDetailView)e.Application().FindModelView(viewId),isRoot);
            e.ShowViewParameters.TargetWindow = targetWindow;
            return detailView;
        }
    }
}