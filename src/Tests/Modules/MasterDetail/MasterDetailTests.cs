using System.Collections;
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
using Xpand.Source.Extensions.XAF.Action;
using Xpand.Source.Extensions.XAF.Frame;
using Xpand.XAF.Agnostic.Tests.Artifacts;
using Xpand.XAF.Agnostic.Tests.Modules.MasterDetail.BOModel;
using Xpand.XAF.Modules.MasterDetail;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Controllers;
using Xunit;

namespace Xpand.XAF.Agnostic.Tests.Modules.MasterDetail{
    [Collection(nameof(XafTypesInfo))]
    public class MasterDetailTests:MasterDetailBaseTests {
   
        [Fact]
        public  void When_model_dashboardView_has_listview_detailview_for_the_same_type_is_masterdetail_enabled(){
            var xafApplication = DefaultMasterDetailModule().Application;
            var modelDashboardView = NewModelDashboardView(xafApplication);

            ((IModelDashboardViewMasterDetail) modelDashboardView).MasterDetail.ShouldBe(true);
            
        }

        [Fact]
        public async void Monitor_ListView_DetailView_dashboardViewItem_pair(){
            var xafApplication = DefaultMasterDetailModule().Application;
            var modelDashboardView = NewModelDashboardView(xafApplication);

            var masterDetailDashoardViewItems = MasterDetailService.MasterDetailDashboardViewItems.Replay();
            masterDetailDashoardViewItems.Connect();

            var dashboardView = xafApplication.CreateDashboardView(xafApplication.CreateObjectSpace(), modelDashboardView.Id, true);
            dashboardView.MockCreateControls();

            var pair = await masterDetailDashoardViewItems.FirstAsync();

            (pair.listViewItem.Model.View as IModelListView).ShouldNotBeNull();
            (pair.detailViewItem.Model.View as IModelDetailView).ShouldNotBeNull();
        }

        [Fact]
        public async Task Handle_listView_process_selected_object_action(){
            var xafApplication = DefaultMasterDetailModule().Application;
            var modelDashboardView = NewModelDashboardView(xafApplication);
            var masterDetailDashoardViewItems = MasterDetailService.MasterDetailDashboardViewItems.Replay();
            masterDetailDashoardViewItems.Connect();
            var dashboardView = xafApplication.CreateDashboardView(xafApplication.CreateObjectSpace(), modelDashboardView.Id, true);
            dashboardView.MockCreateControls();
            var viewItems = await masterDetailDashoardViewItems.FirstAsync();
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

            (await customProcessSelectedItem.FirstAsync()).e.Handled.ShouldBe(true);
        }

        [Fact]
        public async Task<(DashboardViewItem listViewItem, DashboardViewItem detailViewItem)> When_list_view_selection_changed_synchronize_detailview_current_object(){
            var tuple = await ViewItems();
            var listView = ((ListView) tuple.listViewItem.InnerView);
            var md = listView.ObjectSpace.CreateObject<Md>();
            listView.ObjectSpace.CommitChanges();
            listView.Editor.GetMock().Setup(editor => editor.GetSelectedObjects()).Returns(() => new[]{md});
            listView.Editor.CallMethod("OnSelectionChanged");

            var detailView = ((DetailView) tuple.detailViewItem.InnerView);
            detailView.ObjectSpace.GetKeyValue(detailView.CurrentObject).ShouldBe(md.Oid);
            return tuple;
        }

        [Fact]
        public async Task Master_Detail_Save_Action_is_active_when_detailview(){
            var valueTuple = await  ViewItems();

            valueTuple.detailViewItem.Frame.Actions()
                .First(_ => _.Id==MasterDetailService.MasterDetailSaveAction).Active[MasterDetailModule.CategoryName]
                .ShouldBe(true);
        }

        [Fact]
        public async Task Refresh_listview_when_detailview_objectspace_commited(){
            var tuple = await When_list_view_selection_changed_synchronize_detailview_current_object();

            ((Md) tuple.detailViewItem.InnerView.CurrentObject).PropertyName = "updated";
            tuple.detailViewItem.InnerView.ObjectSpace.CommitChanges();

            ((IEnumerable) ((ProxyCollection) ((ListView) tuple.listViewItem.InnerView).CollectionSource.Collection).OriginalCollection)
                .Cast<Md>()
                .Select(md1 => md1.PropertyName).First()
                .ShouldBe("updated");
        }

        [Fact] 
        public async Task Configure_conditional_detailviews(){
            var tuple = await When_list_view_selection_changed_synchronize_detailview_current_object();

            var detailViewObjectTypeLinks = ((IModelApplicationMasterDetail) Application.Model).DashboardMasterDetail.ObjectTypeLinks;
            var objectTypeLink = detailViewObjectTypeLinks.AddNode<IModelMasterDetailViewObjectTypeLink>();
            objectTypeLink.ModelClass = Application.Model.BOModel.GetClass(typeof(MdParent));
            objectTypeLink.DetailView = objectTypeLink.ModelClass.DefaultDetailView;

            var listView = ((ListView) tuple.listViewItem.InnerView);
            var mdParent = listView.ObjectSpace.CreateObject<MdParent>();
            listView.ObjectSpace.CommitChanges();
            listView.Editor.GetMock().Setup(editor => editor.GetSelectedObjects()).Returns(() => new[]{mdParent});
            var detailView = Application.WhenDetailViewCreated().FirstAsync(_ => _.e.View.ObjectTypeInfo.Type==typeof(MdParent)).Replay();
            detailView.Connect();
            listView.Editor.CallMethod("OnSelectionChanged");

            await detailView;

        }
    }

    public class MasterDetailBaseTests:BaseTest{
        protected async Task<(DashboardViewItem listViewItem, DashboardViewItem detailViewItem)> ViewItems(XafApplication xafApplication){
            var modelDashboardView = NewModelDashboardView(xafApplication);
            var masterDetailDashoardViewItems = MasterDetailService.MasterDetailDashboardViewItems.Replay();
            masterDetailDashoardViewItems.Connect();
            var dashboardView = xafApplication.CreateDashboardView(xafApplication.CreateObjectSpace(), modelDashboardView.Id, true);
            dashboardView.MockCreateControls();
            var tuple = await masterDetailDashoardViewItems.FirstAsync();
            return tuple;
        }

        protected async Task<(DashboardViewItem listViewItem, DashboardViewItem detailViewItem)> ViewItems(){
            var xafApplication = DefaultMasterDetailModule().Application;
            return await ViewItems(xafApplication);
        }

        protected static IModelDashboardView NewModelDashboardView(XafApplication xafApplication){
            var modelDashboardView = xafApplication.Model.Views.AddNode<IModelDashboardView>();
            var modelClass = xafApplication.Model.BOModel.GetClass(typeof(Md));
            var modelListViewItem = modelDashboardView.Items.AddNode<IModelDashboardViewItem>();
            modelListViewItem.View = modelClass.DefaultListView;
            var modelDetailViewItem = modelDashboardView.Items.AddNode<IModelDashboardViewItem>();
            modelDetailViewItem.View = modelClass.DefaultDetailView;
            return modelDashboardView;
        }

        public MockedXafApplication Application{ get; private set; }

        protected MasterDetailModule DefaultMasterDetailModule(){
            Application = new XafApplicationMock().Object;
            var masterDetailModule = new MasterDetailModule();
            masterDetailModule.AdditionalExportedTypes.AddRange(new[]{typeof(Md),typeof(MdParent)});
            Application.SetupDefaults(masterDetailModule);
            return masterDetailModule;
        }

        public override void Dispose(){
            Application?.Dispose();
            base.Dispose();
        }
    }
}