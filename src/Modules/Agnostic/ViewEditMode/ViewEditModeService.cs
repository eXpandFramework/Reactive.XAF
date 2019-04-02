using System;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.ViewEditMode{
    public static class ViewEditModeService{
        internal static IObservable<Unit> Connect(){
            return ViewEditModeAssigned.ToUnit().Merge(ViewEditModeChanged.ToUnit());
        }

        public static IObservable<DetailView> ViewEditModeAssigned{ get; } = RxApp.Application.DetailViewCreated()
            .Select(_ => {
                var detailView = _.e.View;
                var viewEditMode = ((IModelDetailViewViewEditMode) detailView.Model).ViewEditMode;
                if (viewEditMode != null){
                    detailView.ViewEditMode = viewEditMode.Value;
                    return detailView;
                }

                return null;
            }).WhenNotDefault();

        public static IObservable<DetailView> ViewEditModeChanged{ get; } = ViewEditModeAssigned
            .ViewEditModeChanging()
            .Select(_ => {
                _.e.Cancel = ((IModelDetailViewViewEditMode) _.detailView.Model).LockViewEditMode;
                return _.detailView;
            });
    }
}