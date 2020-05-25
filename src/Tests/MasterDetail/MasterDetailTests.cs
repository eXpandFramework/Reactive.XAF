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
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.Action;
using Xpand.Extensions.XAF.CollectionSource;
using Xpand.Extensions.XAF.Frame;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.TestsLib;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.MasterDetail.Tests.BOModel;
using Xpand.XAF.Modules.MasterDetial.Tests.BOModel;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Controllers;

namespace Xpand.XAF.Modules.MasterDetail.Tests{
    [NonParallelizable]
    public class MasterDetailTests:BaseTest {

        [XpandTest]
        [TestCase(nameof(Platform.Web))]
        [TestCase(nameof(Platform.Win))]
        public void When_model_dashboardView_has_listview_detailview_for_the_same_type_is_masterdetail_enabled(string platformName){
            var platform = GetPlatform(platformName);
            using (var xafApplication = DefaultMasterDetailModule(platform).Application){
                var modelDashboardView = xafApplication.Model.NewMasterDetailModelDashboardView(typeof(Md));
                ((IModelDashboardViewMasterDetail) modelDashboardView).MasterDetail.ShouldBe(true);
            }
        }
        [XpandTest]
        [TestCase(nameof(Platform.Web))]
        [TestCase(nameof(Platform.Win))]
        public async Task Monitor_ListView_DetailView_dashboardViewItem_pair(string platformName){
            var platform = GetPlatform(platformName);
            using (var xafApplication = DefaultMasterDetailModule(platform).Application){
                var masterDetailDashoardViewItems = xafApplication.WhenMasterDetailDashboardViewItems().Replay();
                masterDetailDashoardViewItems.Connect();
                var modelDashboardView = xafApplication.Model.NewMasterDetailModelDashboardView(typeof(Md));

                var window = xafApplication.CreateWindow(TemplateContext.View, new List<Controller>(), true);
                var dashboardView = xafApplication.CreateDashboardView(xafApplication.CreateObjectSpace(), modelDashboardView.Id, true);
                dashboardView.MockCreateControls();
                window.SetView(dashboardView);

                var pair = await masterDetailDashoardViewItems.FirstAsync().WithTimeOut();

                (pair.listViewItem.Model.View as IModelListView).ShouldNotBeNull();
                (pair.detailViewItem.Model.View as IModelDetailView).ShouldNotBeNull();        
            }
        }
        [XpandTest]
        [TestCase(nameof(Platform.Web))]
        [TestCase(nameof(Platform.Win))]
        public async Task Handle_listView_process_selected_object_action(string platformName){
            var platform = GetPlatform(platformName);
            using (var xafApplication = DefaultMasterDetailModule(platform).Application){
                var modelDashboardView = xafApplication.Model.NewMasterDetailModelDashboardView(typeof(Md));
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

            
        }
        [XpandTest]
        [TestCase(nameof(Platform.Web))]
        [TestCase(nameof(Platform.Win))]
        public async Task When_list_view_selection_changed_synchronize_detailview_current_object(string platformName){
            var platform = GetPlatform(platformName);
            var dashboardViewItemInfo = await When_list_view_selection_changed_synchronize_detailview_current_object_Core(platform).WithTimeOut();
            dashboardViewItemInfo.DetailViewItem.Frame.Application.Dispose();
        }

        private static async Task<DashboardViewItemInfo> When_list_view_selection_changed_synchronize_detailview_current_object_Core(Platform platform){
            var xafApplication = DefaultMasterDetailModule(platform).Application;
            var tuple = await ViewItems(xafApplication).WithTimeOut();
            var listView = ((ListView) tuple.ListViewItem.InnerView);
            var md = listView.ObjectSpace.CreateObject<Md>();
            listView.ObjectSpace.CommitChanges();
            
            listView.Editor.GetMock().Setup(editor => editor.GetSelectedObjects()).Returns(() => new[]{md});
            listView.Editor.OnSelectionChanged();

            var detailView = ((DetailView) tuple.DetailViewItem.InnerView);
            detailView.ObjectSpace.GetKeyValue(detailView.CurrentObject).ShouldBe(md.Oid);
            return tuple;
        }

        [XpandTest]
        [TestCase(nameof(Platform.Web))]
        [TestCase(nameof(Platform.Win))]
        public async Task Master_Detail_Save_Action_is_active_when_detailview(string platformName){
            var platform = GetPlatform(platformName);
            using (var xafApplication = DefaultMasterDetailModule(platform).Application){
                var valueTuple = await ViewItems(xafApplication).WithTimeOut();
                valueTuple.DetailViewItem.Frame.Actions()
                    .First(action => action.Id == MasterDetailService.MasterDetailSaveAction)
                    .Active[MasterDetailModule.CategoryName]
                    .ShouldBe(true);
            }

            
        }
        [XpandTest]
        [TestCase(nameof(Platform.Web))]
        [TestCase(nameof(Platform.Win))]
        public async Task Refresh_listview_object_when_detailview_objectspace_commited(string platformName){
            var platform = GetPlatform(platformName);
            var info = await When_list_view_selection_changed_synchronize_detailview_current_object_Core(platform).WithTimeOut();
            ((Md) info.DetailViewItem.InnerView.CurrentObject).PropertyName = "updated";
            info.DetailViewItem.InnerView.ObjectSpace.CommitChanges();

            ((IEnumerable) ((ProxyCollection) ((ListView) info.ListViewItem.InnerView).CollectionSource.Collection)
                    .OriginalCollection)
                .Cast<Md>()
                .Select(md1 => md1.PropertyName).First()
                .ShouldBe("updated");
            info.DetailViewItem.Frame.Application.Dispose();
        }
        [XpandTest]
        [TestCase(nameof(Platform.Web))]
        [TestCase(nameof(Platform.Win))]
        public async Task Conditional_detailviews(string platformName){
            var platform = GetPlatform(platformName);
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
            var detailView = application.WhenDetailViewCreated().ToDetailView().Replay();
            detailView.Connect();
            listView.Editor.CallMethod("OnSelectionChanged");

            await detailView.FirstAsync(view => view.ObjectTypeInfo.Type==typeof(MdParent)).WithTimeOut();

            var firstObject = listView.CollectionSource.Objects<Md>().First();
            listView.Editor.GetMock().Setup(editor => editor.GetSelectedObjects()).Returns(() => new[]{firstObject});
            listView.Editor.CallMethod("OnSelectionChanged");
            
            await detailView.FirstAsync(view => view.ObjectTypeInfo?.Type==typeof(Md)).WithTimeOut();
            
            application.Dispose();
        }

        static async Task<DashboardViewItemInfo> ViewItems(XafApplication xafApplication){
            var modelDashboardView = xafApplication.Model.NewMasterDetailModelDashboardView(typeof(Md));
            var masterDetailDashoardViewItems = xafApplication.WhenMasterDetailDashboardViewItems().Replay();
            masterDetailDashoardViewItems.Connect();
            var dashboardView = xafApplication.CreateDashboardView(xafApplication.CreateObjectSpace(), modelDashboardView.Id, true);
            dashboardView.MockCreateControls();
            var tuple = await masterDetailDashoardViewItems.FirstAsync().WithTimeOut();
            return new DashboardViewItemInfo(){ListViewItem=tuple.listViewItem,DetailViewItem=tuple.detailViewItem};
        }

        private static MasterDetailModule DefaultMasterDetailModule(Platform platform){
            var application = platform.NewApplication<MasterDetailModule>();
            var masterDetailModule = application.AddModule<MasterDetailModule>(TestContext.CurrentContext.Test.FullName,typeof(Md), typeof(MdParent));
            application.Logon();
            return masterDetailModule;
        }

    }

    internal class DashboardViewItemInfo{
        public DashboardViewItem ListViewItem{ get; set; }
        public DashboardViewItem DetailViewItem{ get; set; }
    }
}