using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Web.SystemModule;
using Shouldly;
using TestsLib;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.ViewEditMode.Tests.BOModel;
using Xunit;

namespace Xpand.XAF.Modules.ViewEditMode.Tests{
    [Collection(nameof(ViewEditModeModule))]
    public class ViewEditModeTests : BaseTest{

        [Theory]
        [InlineData(true,DevExpress.ExpressApp.Editors.ViewEditMode.Edit,Platform.Win)]
        [InlineData(false,DevExpress.ExpressApp.Editors.ViewEditMode.View,Platform.Win)]
        [InlineData(true,DevExpress.ExpressApp.Editors.ViewEditMode.Edit,Platform.Web)]
        [InlineData(false,DevExpress.ExpressApp.Editors.ViewEditMode.View,Platform.Web)]
        internal void Change_ViewEditMode_when_detailview_created(bool lockViewEditMode,DevExpress.ExpressApp.Editors.ViewEditMode viewEditMode,Platform platform){
            using (var application = DefaultViewEditModeModule(platform,nameof(Change_ViewEditMode_when_detailview_created)).Application){
                var editMode = DevExpress.ExpressApp.Editors.ViewEditMode.Edit;
                var viewViewEditMode = ((IModelDetailViewViewEditMode) application.Model.BOModel.GetClass(typeof(VEM)).DefaultDetailView);
                viewViewEditMode.ViewEditMode=editMode;
                viewViewEditMode.LockViewEditMode = lockViewEditMode;
                var viewEditModeChanged = application.WhenViewEditModeAssigned()
                    .ViewEditModeChanging()
                    .Select(_ => {
                        _.e.Cancel = lockViewEditMode;
                        return _;
                    })
                    .Replay(1);
                using (viewEditModeChanged.Connect()){
                    var objectSpace = application.CreateObjectSpace();
                    var detailView = application.CreateDetailView(objectSpace, objectSpace.CreateObject<VEM>());
                    detailView.ViewEditMode.ShouldBe(editMode);
                    detailView.ViewEditMode=DevExpress.ExpressApp.Editors.ViewEditMode.View;
                    detailView.ViewEditMode.ShouldBe(viewEditMode);
                }
            }
        }

        [Fact]
        public async Task UnLock_ViewEditoMode_When_SwitchToEditMode_Action_Executed(){
            using (var application = DefaultViewEditModeModule(Platform.Web,nameof(UnLock_ViewEditoMode_When_SwitchToEditMode_Action_Executed)).Application){
                var viewViewEditMode = ((IModelDetailViewViewEditMode) application.Model.BOModel.GetClass(typeof(VEM)).DefaultDetailView);
                viewViewEditMode.ViewEditMode=DevExpress.ExpressApp.Editors.ViewEditMode.View;

                var objectView = application.CreateObjectView<DetailView>(typeof(VEM));
                objectView.CurrentObject = objectView.ObjectSpace.CreateObject<VEM>();
                var window = application.CreateWindow(TemplateContext.View, null, true);
                
                var webModificationsController = window.GetController<WebModificationsController>();
                var simpleAction = webModificationsController.EditAction;
                window.SetView(objectView);
            
                simpleAction.DoExecute();

                await Task.Delay(1000);
                objectView.ViewEditMode.ShouldBe(DevExpress.ExpressApp.Editors.ViewEditMode.Edit);
            
                ((IModelDetailViewViewEditMode) objectView.Model).LockViewEditMode.ShouldBe(true);
            }


        }

        private static ViewEditModeModule DefaultViewEditModeModule(Platform platform,string title){
            var application = platform.NewApplication<ViewEditModeModule>();
            application.Title = title;
            var viewEditModeModule = new ViewEditModeModule();
            viewEditModeModule.AdditionalExportedTypes.AddRange(new[]{typeof(VEM)});
            application.SetupDefaults(viewEditModeModule);
            application.Logon();
            return viewEditModeModule;
        }
    }
}