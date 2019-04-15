using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base;
using Xpand.Source.Extensions.XAF.ApplicationModulesManager;
using Xpand.Source.Extensions.XAF.Frame;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Controllers;

namespace Xpand.XAF.Modules.MasterDetail{
    public static class MasterDetailService{
        public const string MasterDetailSaveAction = "MasterDetailSaveAction";


        internal static IObservable<Unit> Connect(this ApplicationModulesManager applicationModulesManager){
            var listViewProcessSelectedItem = ListViewProcessSelectedItem.Publish().AutoConnect();
            var connect = SynchronizeDetailView
                .Merge(listViewProcessSelectedItem.ToUnit())
                .Merge(DisableListViewController("ListViewFastCallbackHandlerController"))
                .Merge(DisableDetailViewViewController("ActionsFastCallbackHandlerController"))
                .Merge(SaveAction)
                .Merge(RefreshListView)
                .ToUnit();
            return applicationModulesManager.RegisterActions().ToUnit()
                .Concat(connect)
                .TakeUntilDisposingMainWindow();
        }

        private static IObservable<Unit> RefreshListView{
            get{
                return MasterDetailDashboardViewItems
                    .SelectMany(_ => _.detailViewItem.InnerView.ObjectSpace.WhenCommited()
                            .Select(o => _.listViewItem.InnerView.ObjectSpace.Refresh())
                    )
                    .ToUnit();
            }
        }

        private static IObservable<Unit> SaveAction{
            get{
                return MasterDetailDashboardViewItems
                    .Do(_ => _.detailViewItem.Frame.Actions().First(action => action.Id == MasterDetailSaveAction).Active[MasterDetailModule.CategoryName] = true)
                    .Select(_ => _.detailViewItem.Frame.Actions<SimpleAction>().Where(action => action.Id==MasterDetailSaveAction)
                        .Select(action => action.WhenExecuted()).Merge()
                        .Do(tuple => {
                            tuple.objectSpace.CommitChanges();
                        }))
                    .Merge().ToUnit();
            }
        }

        public static IObservable<DashboardView> DashboardViewCreated{ get; } = RxApp.Application
            .WhenModule(typeof(MasterDetailModule))
            .DashboardViewCreated()
            .Where(_ => ((IModelDashboardViewMasterDetail) _.Model).MasterDetail)
            .Publish()
            .AutoConnect();

        public static IObservable<(DashboardViewItem listViewItem, DashboardViewItem detailViewItem)> MasterDetailDashboardViewItems{get;}=DashboardViewCreated
            .SelectMany(_ => _.WhenControlsCreated().Do(tuple => {},() => {}))
            .SelectMany(_ => _.view.GetItems<DashboardViewItem>().Where(item => item.Model.View is IModelListView)
                .SelectMany(listViewItem => _.view
                    .GetItems<DashboardViewItem>().Where(viewItem 
                        =>viewItem.Model.View is IModelDetailView && viewItem.Model.View.AsObjectView.ModelClass ==listViewItem.Model.View.AsObjectView.ModelClass)
                    .Select(detailViewItem => (listViewItem, detailViewItem))
                )
            )
        .Publish().RefCount();

        private static IObservable<Unit> SynchronizeDetailView{ get; } = MasterDetailDashboardViewItems
            .Select(tuple => tuple)
            .CombineLatest(RxApp.NestedFrames.Select(frame => frame),DashboardViewCreated.Select(view => view), (_, frame, dashboardView) => {
                var listView = ((ListView) _.listViewItem.InnerView);
                if (listView == null)
                    return Observable.Never<Unit>();
                var dashboardViewItem = _.detailViewItem;
                var detailView = ((DetailView) dashboardViewItem.InnerView);
                return listView.WhenSelectionChanged()
                    .Select(tuple => listView.SelectedObjects.Cast<object>().FirstOrDefault())
                    .DistinctUntilChanged()
                    .Select(o => CreateDetailView(detailView, o, listView, dashboardViewItem, frame).CurrentObject)
                    .ToUnit();
            })
            .Merge().ToUnit();

        private static DetailView CreateDetailView(DetailView detailView, object o, ListView listView,
            DashboardViewItem dashboardViewItem, Frame frame){
            var objectTypeLink = ((IModelApplicationMasterDetail) detailView.Model.Application).DashboardMasterDetail
                .ObjectTypeLinks
                .FirstOrDefault(link => {
                    if (link.ModelClass.TypeInfo.Type == o.GetType()){
                        var fitForCriteria =
                            listView.ObjectSpace.IsObjectFitForCriteria(o, CriteriaOperator.Parse(link.Criteria));
                        var b = !fitForCriteria.HasValue || fitForCriteria.Value;
                        return b;
                    }

                    return false;
                });
            if (objectTypeLink != null){
                detailView.Close();
                dashboardViewItem.Frame.SetView(null);
                var application = dashboardViewItem.Frame.Application;
                var objectSpace = application.CreateObjectSpace();
                detailView = application.CreateDetailView(objectSpace, objectTypeLink.DetailView.Id,
                    true, dashboardViewItem.InnerView);
                dashboardViewItem.Frame.SetView(detailView, true, frame);
            }

            detailView.CurrentObject = detailView.ObjectSpace.GetObject(o);
            return detailView;
        }

        public static IObservable<((DashboardViewItem detailViewItem,DashboardViewItem listViewItem) masterDetailItem, CustomProcessListViewSelectedItemEventArgs e)> 
            ListViewProcessSelectedItem{ get;} =MasterDetailDashboardViewItems
            .SelectMany(tuple => tuple.listViewItem.Frame
                .GetController<ListViewProcessCurrentObjectController>()
                .WhenCustomProcessSelectedItem()
                .Do(_ => _.e.Handled = true)
                .Select(_ => (_: tuple, _.e)))
            .Publish().AutoConnect();


        static IObservable<ActionBase> RegisterActions(this ApplicationModulesManager applicationModulesManager){
            return applicationModulesManager.RegisterViewAction(MasterDetailSaveAction, _ => {
                var simpleAction =
                    new SimpleAction(_.controller, _.id, PredefinedCategory.Edit.ToString()){
                        Caption = "Save",
                        ImageName = "MenuBar_Save",
                        TargetViewType = ViewType.DetailView
                    };
                simpleAction.Active[MasterDetailModule.CategoryName] = false;
                return simpleAction;
            }).AsObservable().FirstAsync();
        }

        static IObservable<Unit> DisableListViewController(string typeName){
            return MasterDetailDashboardViewItems
                .SelectMany(_ => _.listViewItem.Frame.Controllers.Cast<Controller>().Where(controller => controller.GetType().Name==typeName))
                .Do(controller => controller.Active[MasterDetailModule.CategoryName]=false).ToUnit();
        }
        static IObservable<Unit> DisableDetailViewViewController(string typeName){
            return MasterDetailDashboardViewItems
                .SelectMany(_ => _.detailViewItem.Frame.Controllers.Cast<Controller>().Where(controller => controller.GetType().Name==typeName))
                .Do(controller => controller.Active[MasterDetailModule.CategoryName]=false).ToUnit();
        }
    }
}