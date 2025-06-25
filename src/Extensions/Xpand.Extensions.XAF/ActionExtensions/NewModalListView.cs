using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.XAF.XafApplicationExtensions;

namespace Xpand.Extensions.XAF.ActionExtensions {
    public static partial class ActionExtensions {

        public static DialogController NewDialogController(this ActionBaseEventArgs e, Type objectType,ViewType viewType,string viewId=null) {
            e.ShowViewParameters.CreatedView =viewType == ViewType.ListView? e.Application().NewListView(objectType,viewId):e.Application().NewDetailView(space => space.CreateObject(objectType));
            e.ShowViewParameters.CreateAllControllers = true;
            e.ShowViewParameters.TargetWindow = TargetWindow.NewModalWindow;
            var dialogController = e.Application().CreateController<DialogController>();
            e.ShowViewParameters.Controllers.Add(dialogController);
            return dialogController;
        }
    }
}