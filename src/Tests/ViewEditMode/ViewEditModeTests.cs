using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Web.SystemModule;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.TestsLib;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.ViewEditMode.Tests.BOModel;

[assembly:XpandTimeout]
namespace Xpand.XAF.Modules.ViewEditMode.Tests{
    [NonParallelizable]
    public class ViewEditModeTests : BaseTest{

        
        [TestCase(true,DevExpress.ExpressApp.Editors.ViewEditMode.Edit,nameof(Platform.Win))]
        [TestCase(false,DevExpress.ExpressApp.Editors.ViewEditMode.View,nameof(Platform.Win))]
        [TestCase(true,DevExpress.ExpressApp.Editors.ViewEditMode.Edit,nameof(Platform.Web))]
        [TestCase(false,DevExpress.ExpressApp.Editors.ViewEditMode.View,nameof(Platform.Web))]
        public void Change_ViewEditMode_when_detailview_created(bool lockViewEditMode,DevExpress.ExpressApp.Editors.ViewEditMode viewEditMode,string platformName){
            var platform = GetPlatform(platformName);
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

        [Test]
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