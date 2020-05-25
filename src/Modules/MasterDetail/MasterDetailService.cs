using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base;
using JetBrains.Annotations;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.Frame;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Reactive.Services.Controllers;

namespace Xpand.XAF.Modules.MasterDetail{
    public static class MasterDetailService{
        public const string MasterDetailSaveAction = "MasterDetailSaveAction";
        internal static IObservable<TSource> TraceMasterDetailModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
            source.Trace(name, MasterDetailModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);


        internal static IObservable<Unit> Connect(this ApplicationModulesManager applicationModulesManager,XafApplication application){
            if (application==null)
                return Observable.Empty<Unit>();
            var listViewProcessSelectedItem = application.WhenMasterDetailListViewProcessSelectedItem().Publish().AutoConnect();
            return application.WhenSynchronizeDetailView()
                .Merge(listViewProcessSelectedItem.ToUnit())
                .Merge(application.DisableListViewController("ListViewFastCallbackHandlerController"))
                .Merge(application.DisableDetailViewViewController("ActionsFastCallbackHandlerController"))
                .Merge(application.WhenSaveAction())
                .Merge(application.WhenRefreshListView())
                .Merge(applicationModulesManager.RegisterActions().ToUnit())
                .ToUnit();
        }

        private static IObservable<Unit> WhenRefreshListView(this XafApplication application) =>
            application.WhenMasterDetailDashboardViewItems()
                .SelectMany(_ => _.detailViewItem.InnerView.ObjectSpace.WhenCommited()
                    .Select(tuple => _.listViewItem.InnerView.ObjectSpace)
                    .Select(objectSpace => {
                        if (_.listViewItem.Frame.Application.GetPlatform() == Platform.Win){
                            objectSpace.ReloadObject(objectSpace.GetObject(_.detailViewItem.InnerView.CurrentObject));
                        }
                        else{
                            objectSpace.Refresh();
                        }

                        return Unit.Default;
                    })
                )
                .ToUnit()
                .Retry(application);

        private static IObservable<Unit> WhenSaveAction(this XafApplication application) =>
            application.WhenMasterDetailDashboardViewItems()
                .Do(_ => _.detailViewItem.Frame.Actions().First(action => action.Id == MasterDetailSaveAction)
                    .Active[MasterDetailModule.CategoryName] = true)
                .SelectMany(_ => _.detailViewItem.Frame.Actions<SimpleAction>().Where(action => action.Id == MasterDetailSaveAction)
                    .Select(action => action.WhenExecuted()).Merge()
                    .Do(tuple => { tuple.Action.Controller.Frame.View.ObjectSpace.CommitChanges(); }))
                .TraceMasterDetailModule(_ => _.Action.Id)
                .Retry(application)
                .ToUnit();

        public static IObservable<DashboardView> WhenMasterDetailDashboardViewCreated(this XafApplication application) =>
            application.WhenDashboardViewCreated()
                .Where(_ => ((IModelDashboardViewMasterDetail) _.Model).MasterDetail);

        public static IObservable<(DashboardViewItem listViewItem, DashboardViewItem detailViewItem)> WhenMasterDetailDashboardViewItems(this XafApplication application,Type objectType=null) =>
            application.WhenMasterDetailDashboardViewCreated()
                .SelectMany(_ => _.WhenControlsCreated().Do(tuple => {},() => {}))
                .SelectMany(_ => _.GetItems<DashboardViewItem>()
                    .Where(item => item.Model.View is IModelListView&&(objectType==null||item.Model.View.AsObjectView.ModelClass.TypeInfo.Type ==objectType))
                    .SelectMany(listViewItem => _.GetItems<DashboardViewItem>().Where(viewItem 
                            =>viewItem.Model.View is IModelDetailView && viewItem.Model.View.AsObjectView.ModelClass ==listViewItem.Model.View.AsObjectView.ModelClass)
                        .Select(detailViewItem => (listViewItem, detailViewItem))
                    )
                )
                .TraceMasterDetailModule(_ =>$"{_.detailViewItem.Model.Id}, {_.listViewItem.Model.Id}" );

        private static IObservable<Unit> WhenSynchronizeDetailView(this XafApplication application) =>
            application.WhenMasterDetailDashboardViewItems()
                .CombineLatest(application.WhenNestedFrameCreated(),application.WhenMasterDetailDashboardViewCreated().Select(view => view), (_, frame, dashboardView) => {
                    var listView = ((ListView) _.listViewItem.InnerView);
                    if (listView == null)
                        return Observable.Never<Unit>();
                    var dashboardViewItem = _.detailViewItem;
                    var detailView = ((DetailView) dashboardViewItem.InnerView);
                    return listView.WhenSelectionChanged()
                        .Select(tuple => listView.SelectedObjects.Cast<object>().FirstOrDefault())
                        .WhenNotDefault()
                        .DistinctUntilChanged(o => listView.ObjectSpace.GetKeyValue(o))
                        .Select(o => detailView.SynchronizeCurrentObject(o, listView, dashboardViewItem, frame))
                        .TraceMasterDetailModule(view => view.Id)
                        .ToUnit();
                })
                .Merge().ToUnit()
                .Retry(application);

        private static DetailView SynchronizeCurrentObject(this DetailView detailView,object o, ListView listView, DashboardViewItem dashboardViewItem, NestedFrame frame){
            var objectTypeLink = detailView.GetObjectTypeLink(o, listView);
            if (objectTypeLink != null){
                detailView = objectTypeLink.CreateDetailView(detailView, dashboardViewItem, frame);
            }

            if (detailView.ObjectSpace == null){
                dashboardViewItem.Frame.SetView(null);
                detailView = (DetailView) frame.Application.NewView(detailView.Model);
                dashboardViewItem.Frame.SetView(detailView);
            }
            detailView.CurrentObject = detailView.ObjectSpace.GetObject(o);
            return detailView;
        }

        private static DetailView CreateDetailView(this IModelMasterDetailViewObjectTypeLink objectTypeLink, DetailView detailView, DashboardViewItem dashboardViewItem, Frame frame){
            detailView.Close();
            dashboardViewItem.Frame.SetView(null);
            var application = dashboardViewItem.Frame.Application;
            var objectSpace = application.CreateObjectSpace();
            detailView = application.CreateDetailView(objectSpace, objectTypeLink.DetailView.Id, true, dashboardViewItem.InnerView);
            dashboardViewItem.Frame.SetView(detailView, true, frame);
            return detailView;
        }

        private static IModelMasterDetailViewObjectTypeLink GetObjectTypeLink(this DetailView detailView, object o, ListView listView) =>
            ((IModelApplicationMasterDetail) detailView.Model.Application).DashboardMasterDetail.ObjectTypeLinks
            .FirstOrDefault(link => {
                if (link.ModelClass.TypeInfo.Type == o.GetType()){
                    var fitForCriteria = listView.ObjectSpace.IsObjectFitForCriteria(o, CriteriaOperator.Parse(link.Criteria));
                    return !fitForCriteria.HasValue || fitForCriteria.Value;
                }
                return false;
            });

        public static IObservable<((DashboardViewItem detailViewItem, DashboardViewItem listViewItem) masterDetailItem, CustomProcessListViewSelectedItemEventArgs e)> 
            WhenMasterDetailListViewProcessSelectedItem(this XafApplication application){
            
            return application.WhenMasterDetailDashboardViewItems()
                .SelectMany(tuple => tuple.listViewItem.Frame
                    .GetController<ListViewProcessCurrentObjectController>()
                    .WhenCustomProcessSelectedItem()
                    .Do(_ => _.e.Handled = true)
                    .Select(_ => (_: tuple, _.e)))
                .Publish().AutoConnect()
                .TraceMasterDetailModule(_ => $"{_._.listViewItem.Id}, {_.e.InnerArgs.CurrentObject}");
        }


        static IObservable<ActionBase> RegisterActions(this ApplicationModulesManager applicationModulesManager) =>
            applicationModulesManager.RegisterViewAction(MasterDetailSaveAction, _ => {
                var simpleAction =
                    new SimpleAction(_.controller, _.id, PredefinedCategory.Edit.ToString()){
                        Caption = "Save",
                        ImageName = "MenuBar_Save",
                        TargetViewType = ViewType.DetailView
                    };
                simpleAction.Active[MasterDetailModule.CategoryName] = false;
                return simpleAction;
            }).TraceMasterDetailModule(action => action.Id);

        [PublicAPI]
        public static IModelDashboardView NewMasterDetailModelDashboardView(this IModelApplication modelApplication, Type objectType){
            var modelDashboardView = modelApplication.Views.AddNode<IModelDashboardView>();
            var modelClass = modelApplication.BOModel.GetClass(objectType);
            var modelListViewItem = modelDashboardView.Items.AddNode<IModelDashboardViewItem>();
            modelListViewItem.View = modelClass.DefaultListView;
            var modelDetailViewItem = modelDashboardView.Items.AddNode<IModelDashboardViewItem>();
            modelDetailViewItem.View = modelClass.DefaultDetailView;
            return modelDashboardView;
        }

        static IObservable<Unit> DisableListViewController(this XafApplication application, string typeName) =>
            application.WhenMasterDetailDashboardViewItems()
                .SelectMany(_ => _.listViewItem.Frame.Controllers.Cast<Controller>().Where(controller => controller.GetType().Name==typeName))
                .Do(controller => controller.Active[MasterDetailModule.CategoryName]=false).ToUnit()
                .TraceMasterDetailModule();

        static IObservable<Unit> DisableDetailViewViewController(this XafApplication application,string typeName) =>
            application.WhenMasterDetailDashboardViewItems()
                .SelectMany(_ => _.detailViewItem.Frame.Controllers.Cast<Controller>().Where(controller => controller.GetType().Name==typeName))
                .Do(controller => controller.Active[MasterDetailModule.CategoryName]=false).ToUnit()
                .TraceMasterDetailModule();
    }
}