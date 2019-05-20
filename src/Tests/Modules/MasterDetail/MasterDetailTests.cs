using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using Fasterflect;
using Moq;
using Shouldly;
using Tests.Artifacts;
using Tests.Modules.MasterDetail.BOModel;
using Xpand.Source.Extensions.XAF.Action;
using Xpand.Source.Extensions.XAF.Frame;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.MasterDetail;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Controllers;
using Xunit;

namespace Tests.Modules.MasterDetail{
    [Collection(nameof(XafTypesInfo))]
    public class MasterDetailTests:BaseTest {

        [Theory]
        [InlineData(Platform.Web)]
        [InlineData(Platform.Win)]
        internal void When_model_dashboardView_has_listview_detailview_for_the_same_type_is_masterdetail_enabled(Platform platform){
            var xafApplication = DefaultMasterDetailModule(platform).Application;
            var modelDashboardView = xafApplication.Model.NewModelDashboardView(typeof(Md));
            ((IModelDashboardViewMasterDetail) modelDashboardView).MasterDetail.ShouldBe(true);
            
        }

        [Theory]
        [InlineData(Platform.Web)]
        [InlineData(Platform.Win)]
        internal async void Monitor_ListView_DetailView_dashboardViewItem_pair(Platform platform){
            var xafApplication = DefaultMasterDetailModule(platform).Application;
            var masterDetailDashoardViewItems = xafApplication.WhenMasterDetailDashboardViewItems().Replay();
            masterDetailDashoardViewItems.Connect();
            var modelDashboardView = xafApplication.Model.NewModelDashboardView(typeof(Md));

            var window = xafApplication.CreateWindow(TemplateContext.View, new List<Controller>(), true);
            var dashboardView = xafApplication.CreateDashboardView(xafApplication.CreateObjectSpace(), modelDashboardView.Id, true);
            dashboardView.MockCreateControls();
            window.SetView(dashboardView);
                

            var pair = await masterDetailDashoardViewItems.FirstAsync().WithTimeOut();

            (pair.listViewItem.Model.View as IModelListView).ShouldNotBeNull();
            (pair.detailViewItem.Model.View as IModelDetailView).ShouldNotBeNull();        }

        [Theory]
        [InlineData(Platform.Web)]
        [InlineData(Platform.Win)]
        internal async Task Handle_listView_process_selected_object_action(Platform platform){
            var xafApplication = DefaultMasterDetailModule(platform).Application;
            var modelDashboardView = xafApplication.Model.NewModelDashboardView(typeof(Md));
            var masterDetailDashoardViewItems = xafApplication.WhenMasterDetailDashboardViewItems().Replay();
            masterDetailDashoardViewItems.Connect();
            var dashboardView = xafApplication.CreateDashboardView(xafApplication.CreateObjectSpace(), modelDashboardView.Id, true);
            dashboardView.MockCreateControls();
            var viewItems = await masterDetailDashoardViewItems.FirstAsync().WithTimeOut();
            var controller = viewItems.listViewItem.Frame.GetController<ListViewProcessCurrentObjectController>();
            controller.ShouldNotBeNull();
            controller.ProcessCurrentObjectAction.Active.Clear();
            var selectionContext = Mock.Of<ISelectionContext>();
            var value = new object();
            Mock.Get(selectionContext).Setup(context => context.CurrentObject).Returns(value);
            controller.ProcessCurrentObjectAction.SelectionContext = selectionContext;
            var customProcessSelectedItem = controller.WhenCustomProcessSelectedItem().Replay();
            customProcessSelectedItem.Connect();

            controller.ProcessCurrentObjectAction.DoTheExecute(true);

            (await customProcessSelectedItem.FirstAsync().WithTimeOut()).e.Handled.ShouldBe(true);            
        }

        [Theory]
        [InlineData(Platform.Web)]
        [InlineData(Platform.Win)]
        internal async Task When_list_view_selection_changed_synchronize_detailview_current_object(Platform platform){
            await When_list_view_selection_changed_synchronize_detailview_current_object_Core(platform).WithTimeOut();        }

        private static async Task<DashboardViewItemInfo> When_list_view_selection_changed_synchronize_detailview_current_object_Core(Platform platform){
            var xafApplication = DefaultMasterDetailModule(platform).Application;
            var tuple = await ViewItems(xafApplication).WithTimeOut();
            var listView = ((ListView) tuple.ListViewItem.InnerView);
            var md = listView.ObjectSpace.CreateObject<Md>();
            listView.ObjectSpace.CommitChanges();
            listView.Editor.GetMock().Setup(editor => editor.GetSelectedObjects()).Returns(() => new[]{md});
            listView.Editor.CallMethod("OnSelectionChanged");

            var detailView = ((DetailView) tuple.DetailViewItem.InnerView);
            detailView.ObjectSpace.GetKeyValue(detailView.CurrentObject).ShouldBe(md.Oid);
            return tuple;
        }


        [Theory]
        [InlineData(Platform.Web)]
        [InlineData(Platform.Win)]
        internal async Task Master_Detail_Save_Action_is_active_when_detailview(Platform platform){
            var xafApplication = DefaultMasterDetailModule(platform).Application;
            var valueTuple = await ViewItems(xafApplication).WithTimeOut();
            valueTuple.DetailViewItem.Frame.Actions()
                .First(action => action.Id == MasterDetailService.MasterDetailSaveAction)
                .Active[MasterDetailModule.CategoryName]
                .ShouldBe(true);
        }

        [Theory]
        [InlineData(Platform.Win)]
        [InlineData(Platform.Web)]
        internal async Task Refresh_listview_object_when_detailview_objectspace_commited(Platform platform){
            var info = await When_list_view_selection_changed_synchronize_detailview_current_object_Core(platform).WithTimeOut();
            ((Md) info.DetailViewItem.InnerView.CurrentObject).PropertyName = "updated";
            info.DetailViewItem.InnerView.ObjectSpace.CommitChanges();

            ((IEnumerable) ((ProxyCollection) ((ListView) info.ListViewItem.InnerView).CollectionSource.Collection)
                    .OriginalCollection)
                .Cast<Md>()
                .Select(md1 => md1.PropertyName).First()
                .ShouldBe("updated");        }

        [Theory]
        [InlineData(Platform.Web)]
        [InlineData(Platform.Win)]
        internal async Task Configure_conditional_detailviews(Platform platform){
            var info = await When_list_view_selection_changed_synchronize_detailview_current_object_Core(platform).WithTimeOut();
            var application = info.DetailViewItem.Frame.Application;
            var detailViewObjectTypeLinks = ((IModelApplicationMasterDetail) application.Model).DashboardMasterDetail.ObjectTypeLinks;
            var objectTypeLink = detailViewObjectTypeLinks.AddNode<IModelMasterDetailViewObjectTypeLink>();
            objectTypeLink.ModelClass = application.Model.BOModel.GetClass(typeof(MdParent));
            objectTypeLink.DetailView = objectTypeLink.ModelClass.DefaultDetailView;

            var listView = ((ListView) info.ListViewItem.InnerView);
            var mdParent = listView.ObjectSpace.CreateObject<MdParent>();
            listView.ObjectSpace.CommitChanges();
            listView.Editor.GetMock().Setup(editor => editor.GetSelectedObjects()).Returns(() => new[]{mdParent});
            var detailView = application.WhenDetailViewCreated().FirstAsync(t => t.e.View.ObjectTypeInfo.Type==typeof(MdParent)).Replay();
            detailView.Connect();
            listView.Editor.CallMethod("OnSelectionChanged");

            await detailView.WithTimeOut();                
        }

        static async Task<DashboardViewItemInfo> ViewItems(XafApplication xafApplication){
            var modelDashboardView = xafApplication.Model.NewModelDashboardView(typeof(Md));
            var masterDetailDashoardViewItems = xafApplication.WhenMasterDetailDashboardViewItems().Replay();
            masterDetailDashoardViewItems.Connect();
            var dashboardView = xafApplication.CreateDashboardView(xafApplication.CreateObjectSpace(), modelDashboardView.Id, true);
            dashboardView.MockCreateControls();
            var tuple = await masterDetailDashoardViewItems.FirstAsync().WithTimeOut();
            return new DashboardViewItemInfo(){ListViewItem=tuple.listViewItem,DetailViewItem=tuple.detailViewItem};
        }

        private static MasterDetailModule DefaultMasterDetailModule(Platform platform){
            var application = platform.NewApplication();
            application.Title = "MasterDetailModule";
            var masterDetailModule = new MasterDetailModule();
            masterDetailModule.AdditionalExportedTypes.AddRange(new[]{typeof(Md),typeof(MdParent)});
            application.SetupDefaults(masterDetailModule);
            return masterDetailModule;
        }

    }

    internal class DashboardViewItemInfo{
        public DashboardViewItem ListViewItem{ get; set; }
        public DashboardViewItem DetailViewItem{ get; set; }
    }
}