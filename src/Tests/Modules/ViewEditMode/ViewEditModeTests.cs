using System.Reactive.Linq;
using DevExpress.ExpressApp;
using Shouldly;
using Xpand.XAF.Agnostic.Tests.Artifacts;
using Xpand.XAF.Agnostic.Tests.Modules.ViewEditMode.BOModel;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.ViewEditMode;
using Xunit;

namespace Xpand.XAF.Agnostic.Tests.Modules.ViewEditMode{
    [Collection(nameof(XafTypesInfo))]
    public class ViewEditModeTests : BaseTest{

        [Theory]
        [InlineData(true,DevExpress.ExpressApp.Editors.ViewEditMode.Edit)]
        [InlineData(false,DevExpress.ExpressApp.Editors.ViewEditMode.View)]
        public void Change_ViewEditMode_when_detailview_created(bool lockViewEditMode,DevExpress.ExpressApp.Editors.ViewEditMode viewEditMode){
            var module = DefaultViewEditModeModule();
            var application = module.Application;
            var editMode = DevExpress.ExpressApp.Editors.ViewEditMode.Edit;
            var viewViewEditMode = ((IModelDetailViewViewEditMode) application.Model.BOModel.GetClass(typeof(VEM)).DefaultDetailView);
            viewViewEditMode.ViewEditMode=editMode;
            viewViewEditMode.LockViewEditMode = lockViewEditMode;
            var viewEditModeChanged = ViewEditModeService.ViewEditModeAssigned
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

        private ViewEditModeModule DefaultViewEditModeModule(){
            var application = new XafApplicationMock().Object;
            var viewEditModeModule = new ViewEditModeModule();
            viewEditModeModule.AdditionalExportedTypes.AddRange(new[]{typeof(VEM)});
            application.SetupDefaults(viewEditModeModule);
            return viewEditModeModule;
        }
    }
}