using System.Reactive;
using System.Reactive.Linq;
using AppDomainToolkit;
using Shouldly;
using Tests.Artifacts;
using Tests.Modules.ViewEditMode.BOModel;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.ViewEditMode;
using Xunit;

namespace Tests.Modules.ViewEditMode{
//    [Collection(nameof(XafTypesInfo))]
    public class ViewEditModeTests : BaseTest{

        [Theory]
        [InlineData(true,DevExpress.ExpressApp.Editors.ViewEditMode.Edit,Platform.Win)]
        [InlineData(false,DevExpress.ExpressApp.Editors.ViewEditMode.View,Platform.Win)]
        [InlineData(true,DevExpress.ExpressApp.Editors.ViewEditMode.Edit,Platform.Web)]
        [InlineData(false,DevExpress.ExpressApp.Editors.ViewEditMode.View,Platform.Web)]
        internal void Change_ViewEditMode_when_detailview_created(bool lockViewEditMode,DevExpress.ExpressApp.Editors.ViewEditMode viewEditMode,Platform platform){
            RemoteFunc.Invoke(Domain, lockViewEditMode,viewEditMode,platform, (l, v, p) => {
                var module = DefaultViewEditModeModule(p);
                var application = module.Application;
                var editMode = DevExpress.ExpressApp.Editors.ViewEditMode.Edit;
                var viewViewEditMode = ((IModelDetailViewViewEditMode) application.Model.BOModel.GetClass(typeof(VEM)).DefaultDetailView);
                viewViewEditMode.ViewEditMode=editMode;
                viewViewEditMode.LockViewEditMode = l;
                var viewEditModeChanged = application.WhenViewEditModeAssigned()
                    .ViewEditModeChanging()
                    .Select(_ => {
                        _.e.Cancel = l;
                        return _;
                    })
                    .Replay();
                viewEditModeChanged.Connect();
                var objectSpace = application.CreateObjectSpace();
                var detailView = application.CreateDetailView(objectSpace, objectSpace.CreateObject<VEM>());
            
                detailView.ViewEditMode.ShouldBe(editMode);
                detailView.ViewEditMode=DevExpress.ExpressApp.Editors.ViewEditMode.View;
                detailView.ViewEditMode.ShouldBe(v);

                return Unit.Default;
            });

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