using System;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using Xpand.Extensions.Reactive.Transform;
using Xpand.TestsLib.Common.BO;
using Xpand.XAF.Modules.CloneModelView;
using Xpand.XAF.Modules.Reactive.Services;

// ReSharper disable once CheckNamespace
namespace TestApplication.ViewWizard {
    public static class ViewWizardService{
        public static IObservable<Unit> ConnectViewWizardService(this ApplicationModulesManager manager) 
            => manager.WhenCustomizeTypesInfo()
                .Do(_ => {
                    var typeInfo = _.e.TypesInfo.FindTypeInfo(typeof(Order));
                    typeInfo.AddAttribute(new CloneModelViewAttribute(CloneViewType.DetailView, "OrderPage1_DetailVIew"));
                }).ToUnit();
    }
}