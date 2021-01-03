using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.CollectionSourceExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common.BO;
using Xpand.XAF.Modules.CloneModelView;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace TestApplication.Module.Web.LookupCascade{
    public static class LookupCascadeService{
        const  string LookupCascadeOrderListView = "LookupCascade_Order_ListView";
        const  string LookupCascadeOrderDetailView = "LookupCascade_Order_DetailView";
        public static IObservable<Unit> LookupCascade(this ApplicationModulesManager manager) => CustomizeTypesInfo(manager).Merge(manager.RegisterActions());

        private static IObservable<Unit> RegisterActions(this ApplicationModulesManager manager) =>
	        manager.RegisterViewPopupWindowShowAction("ShowInPopup")
		        .SelectMany(action => {
			        action.TargetViewId = LookupCascadeOrderListView;
			        return action.WhenCustomizePopupWindowParams().Do(_ => {
				        var application = _.Action.Application;
				        var detailView = application.NewDetailView(((ListView) _.Action.Controller.Frame.View).CollectionSource.Objects<Order>().First(order =>order.Product.ProductName.EndsWith("0") ),
					        (IModelDetailView) application.Model.Views[LookupCascadeOrderDetailView]);
				        detailView.ViewEditMode=ViewEditMode.Edit;
				        _.View = detailView;
			        });
		        })
		        .ToUnit();

        private static IObservable<Unit> CustomizeTypesInfo(this ApplicationModulesManager manager) =>
	        manager.WhenCustomizeTypesInfo()
		        .Do(_ => {
			        var typeInfo = _.e.TypesInfo.FindTypeInfo(typeof(Order));
                    
			        typeInfo.AddAttribute(new CloneModelViewAttribute(CloneViewType.ListView, LookupCascadeOrderListView));
			        typeInfo.AddAttribute(new CloneModelViewAttribute(CloneViewType.DetailView, LookupCascadeOrderDetailView));
			        typeInfo = _.e.TypesInfo.FindTypeInfo(typeof(Accessory));
			        typeInfo.AddAttribute(new CloneModelViewAttribute(CloneViewType.ListView, "LookupCascade_Accessory_LookupListView"));
		        }).ToUnit();
    }
}