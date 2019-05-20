using System.Reactive.Linq;
using DevExpress.ExpressApp;
using Shouldly;
using Tests.Artifacts;
using Tests.Modules.ViewEditMode.BOModel;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.ViewEditMode;
using Xunit;

namespace Tests.Modules.ViewEditMode{
    [Collection(nameof(XafTypesInfo))]
    public class ViewEditModeTests : BaseTest{

        [Theory]
        [InlineData(true,DevExpress.ExpressApp.Editors.ViewEditMode.Edit,Platform.Win)]
        [InlineData(false,DevExpress.ExpressApp.Editors.ViewEditMode.View,Platform.Win)]
        [InlineData(true,DevExpress.ExpressApp.Editors.ViewEditMode.Edit,Platform.Web)]
        [InlineData(false,DevExpress.ExpressApp.Editors.ViewEditMode.View,Platform.Web)]
        internal void Change_ViewEditMode_when_detailview_created(bool lockViewEditMode,DevExpress.ExpressApp.Editors.ViewEditMode viewEditMode,Platform platform){
            var module = DefaultViewEditModeModule(platform);
            var application = module.Application;
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
                .Replay();
            viewEditModeChanged.Connect();
            var objectSpace = application.CreateObjectSpace();
            var detailView = application.CreateDetailView(objectSpace, objectSpace.CreateObject<VEM>());
            
            detailView.ViewEditMode.ShouldBe(editMode);
            detailView.ViewEditMode=DevExpress.ExpressApp.Editors.ViewEditMode.View;
            detailView.ViewEditMode.ShouldBe(viewEditMode);

        }

        private static ViewEditModeModule DefaultViewEditModeModule(Platform platform){
            var application = platform.NewApplication();
            var viewEditModeModule = new ViewEditModeModule();
            viewEditModeModule.AdditionalExportedTypes.AddRange(new[]{typeof(VEM)});
            application.SetupDefaults(viewEditModeModule);
            return viewEditModeModule;
        }
    }
}