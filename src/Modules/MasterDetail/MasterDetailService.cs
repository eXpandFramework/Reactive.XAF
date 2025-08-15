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
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Reactive.Services.Controllers;

namespace Xpand.XAF.Modules.MasterDetail{
    public static class MasterDetailService{
        public const string MasterDetailSaveAction = "MasterDetailSaveAction";
        internal static IObservable<TSource> TraceMasterDetailModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<ITraceEvent> traceAction = null,
            Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,Func<string> allMessageFactory = null,
            [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
            source.Trace(name, ReactiveMasterDetailModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy,allMessageFactory, memberName,sourceFilePath,sourceLineNumber);


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

        private static IObservable<Unit> WhenRefreshListView(this XafApplication application) 
	        => application.WhenMasterDetailDashboardViewItems()
                .SelectManyItemResilient(v => v.detailViewItem.InnerView.ObjectSpace.WhenCommitted()
                    .Select(_ => v.listViewItem.InnerView.ObjectSpace)
                    .Select(objectSpace => {
                        if (v.listViewItem.Frame.Application.GetPlatform() == Platform.Win){
                            objectSpace.ReloadObject(objectSpace.GetObject(v.detailViewItem.InnerView.CurrentObject));
                        }
                        else{
                            objectSpace.Refresh();
                        }

                        return Unit.Default;
                    })
                )
                .ToUnit();

        private static IObservable<Unit> WhenSaveAction(this XafApplication application) 
	        => application.WhenMasterDetailDashboardViewItems()
                .DoItemResilient(t => t.detailViewItem.Frame.Actions().First(action => action.Id == MasterDetailSaveAction)
                    .Active[ReactiveMasterDetailModule.CategoryName] = true)
                .SelectManyItemResilient(v => v.detailViewItem.Frame.Actions<SimpleAction>().Where(action => action.Id == MasterDetailSaveAction)
                    .Select(action => action.WhenExecuted()).Merge()
                    .Do(tuple => { tuple.Action.Controller.Frame.View.ObjectSpace.CommitChanges(); }))
                .TraceMasterDetailModule(e => e.Action.Id)
                .ToUnit();

        public static IObservable<DashboardView> WhenMasterDetailDashboardViewCreated(this XafApplication application) 
	        => application.WhenDashboardViewCreated().Where(v => ((IModelDashboardViewMasterDetail) v.Model).MasterDetail);

        public static IObservable<(DashboardViewItem listViewItem, DashboardViewItem detailViewItem)> WhenMasterDetailDashboardViewItems(this XafApplication application,Type objectType=null) 
	        => application.WhenMasterDetailDashboardViewCreated()
                .SelectMany(dashboardView => dashboardView.WhenControlsCreated().Do(_ => {},() => {}))
                .SelectManyItemResilient(dashboardView => dashboardView.GetItems<DashboardViewItem>()
                    .Where(item => item.Model.View is IModelListView&&(objectType==null||item.Model.View.AsObjectView.ModelClass.TypeInfo.Type ==objectType))
                    .SelectMany(listViewItem => dashboardView.GetItems<DashboardViewItem>().Where(viewItem 
                            =>viewItem.Model.View is IModelDetailView && viewItem.Model.View.AsObjectView.ModelClass ==listViewItem.Model.View.AsObjectView.ModelClass)
                        .Select(detailViewItem => (listViewItem, detailViewItem))
                    )
                )
                .TraceMasterDetailModule(t =>$"{t.detailViewItem.Model.Id}, {t.listViewItem.Model.Id}" );

        private static IObservable<Unit> WhenSynchronizeDetailView(this XafApplication application) 
	        => application.WhenMasterDetailDashboardViewItems()
                .CombineLatest(application.WhenNestedFrameCreated(),application.WhenMasterDetailDashboardViewCreated().Select(view => view), (t, frame, _) => {
                    var listView = ((ListView) t.listViewItem.InnerView);
                    if (listView == null)
                        return Observable.Never<Unit>();
                    var dashboardViewItem = t.detailViewItem;
                    var detailView = ((DetailView) dashboardViewItem.InnerView);
                    return listView.WhenSelectionChanged()
                        .Select(_ => listView.SelectedObjects.Cast<object>().FirstOrDefault())
                        .WhenNotDefault()
                        .DistinctUntilChanged(o => listView.ObjectSpace.GetKeyValue(o))
                        .SelectItemResilient(o => detailView.SynchronizeCurrentObject(o, listView, dashboardViewItem, frame))
                        .TraceMasterDetailModule(view => view.Id)
                        .ToUnit();
                })
                .Merge().ToUnit();

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
            var objectSpace = application.CreateObjectSpace(objectTypeLink.DetailView.ModelClass.TypeInfo.Type);
            detailView = application.CreateDetailView(objectSpace, objectTypeLink.DetailView.Id, true, dashboardViewItem.InnerView);
            dashboardViewItem.Frame.SetView(detailView, true, frame);
            return detailView;
        }

        private static IModelMasterDetailViewObjectTypeLink GetObjectTypeLink(this DetailView detailView, object o, ListView listView) 
	        => ((IModelApplicationMasterDetail) detailView.Model.Application).DashboardMasterDetail.ObjectTypeLinks
            .FirstOrDefault(link => {
                if (link.ModelClass.TypeInfo.Type == o.GetType()){
                    var fitForCriteria = listView.ObjectSpace.IsObjectFitForCriteria(o, CriteriaOperator.Parse(link.Criteria));
                    return !fitForCriteria.HasValue || fitForCriteria.Value;
                }
                return false;
            });

        public static IObservable<((DashboardViewItem listViewItem, DashboardViewItem detailViewItem) _, SimpleActionExecuteEventArgs)> WhenMasterDetailListViewProcessSelectedItem(this XafApplication application) 
	        => application.WhenMasterDetailDashboardViewItems()
		        .SelectMany(tuple => tuple.listViewItem.Frame
			        .GetController<ListViewProcessCurrentObjectController>()
			        .WhenCustomProcessSelectedItem(true)
                    .Select(e => (_: tuple, e)))
		        .Publish().AutoConnect()
		        .TraceMasterDetailModule(t => $"{t._.listViewItem.Id}, ");


        static IObservable<ActionBase> RegisterActions(this ApplicationModulesManager applicationModulesManager) 
	        => applicationModulesManager.RegisterViewAction(MasterDetailSaveAction, t => {
                var simpleAction =
                    new SimpleAction(t.controller, t.id, nameof(PredefinedCategory.Edit)){
                        Caption = "Save",
                        ImageName = "MenuBar_Save",
                        TargetViewType = ViewType.DetailView
                    };
                simpleAction.Active[ReactiveMasterDetailModule.CategoryName] = false;
                return simpleAction;
            }).TraceMasterDetailModule(action => action.Id);

        
        public static IModelDashboardView NewMasterDetailModelDashboardView(this IModelApplication modelApplication, Type objectType){
            var modelDashboardView = modelApplication.Views.AddNode<IModelDashboardView>();
            var modelClass = modelApplication.BOModel.GetClass(objectType);
            var modelListViewItem = modelDashboardView.Items.AddNode<IModelDashboardViewItem>();
            modelListViewItem.View = modelClass.DefaultListView;
            var modelDetailViewItem = modelDashboardView.Items.AddNode<IModelDashboardViewItem>();
            modelDetailViewItem.View = modelClass.DefaultDetailView;
            return modelDashboardView;
        }

        static IObservable<Unit> DisableListViewController(this XafApplication application, string typeName) 
	        => application.WhenMasterDetailDashboardViewItems()
		        .SelectMany(t => t.listViewItem.Frame.Controllers.Cast<Controller>().Where(controller => controller.GetType().Name==typeName))
		        .Do(controller => controller.Active[ReactiveMasterDetailModule.CategoryName]=false).ToUnit()
		        .TraceMasterDetailModule();

        static IObservable<Unit> DisableDetailViewViewController(this XafApplication application,string typeName) 
	        => application.WhenMasterDetailDashboardViewItems()
		        .SelectMany(t => t.detailViewItem.Frame.Controllers.Cast<Controller>().Where(controller => controller.GetType().Name==typeName))
		        .Do(controller => controller.Active[ReactiveMasterDetailModule.CategoryName]=false).ToUnit()
		        .TraceMasterDetailModule();
    }
}