using System;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using Xpand.Extensions.Reactive.Transform;
using Xpand.TestsLib.BO;
using Xpand.XAF.Modules.CloneModelView;
using Xpand.XAF.Modules.Reactive.Services;

public static class ViewWizardService{
    public static IObservable<Unit> ConnectViewWizardService(this ApplicationModulesManager manager){
        // return Observable.Empty<Unit>();
        return manager.WhenCustomizeTypesInfo()
            .Do(_ => {
                var typeInfo = _.e.TypesInfo.FindTypeInfo(typeof(Order));
                typeInfo.AddAttribute(new CloneModelViewAttribute(CloneViewType.DetailView, "OrderPage1_DetailView"));
            }).ToUnit();
    }
}