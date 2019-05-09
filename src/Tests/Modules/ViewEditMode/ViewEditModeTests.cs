using System.Diagnostics;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using Mono.Cecil;
using Mono.Cecil.Pdb;
using Shouldly;
using Xpand.XAF.Agnostic.Tests.Artifacts;
using Xpand.XAF.Agnostic.Tests.Modules.ViewEditMode.BOModel;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.ViewEditMode;
using Xunit;

namespace Xpand.XAF.Agnostic.Tests.Modules.ViewEditMode{
    [Collection(nameof(XafTypesInfo))]
    public class ViewEditModeTests : BaseTest{
        public void MethodName(){
            var readerParameters = new ReaderParameters();
            readerParameters.ReadSymbols = true;
            readerParameters.ReadWrite = true;
            var fileName = @"C:\Work\eXpandFramework\Packages\bin\Xpand.XAF.Modules.CloneModelView.dll";
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(
                fileName,
                readerParameters);
            var writerParameters = new WriterParameters() {WriteSymbols = true};

            assemblyDefinition.Write(writerParameters);
            Process.Start(@"C:\Work\eXpandFramework\Packages\IndexSources.ps1");
        }

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