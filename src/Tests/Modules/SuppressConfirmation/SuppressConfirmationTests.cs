using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Shouldly;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Agnostic.Tests.Artifacts;
using Xpand.XAF.Agnostic.Tests.Modules.SuppressConfirmation.BOModel;
using Xpand.XAF.Modules.MasterDetail;
using Xpand.XAF.Modules.SuppressConfirmation;
using Xunit;

namespace Xpand.XAF.Agnostic.Tests.Modules.SuppressConfirmation{
    [Collection(nameof(XafTypesInfo))]
    public class SuppressConfirmationTests : BaseTest{

        [Theory]
        [InlineData(typeof(ListView))]
        [InlineData(typeof(DetailView))]
        public async Task Signal_When_Window_with_SupressConfirmation_Enabled_ObjectView_changed(Type viewType){
            using (var application = DefaultSuppressConfirmationModule().Application){
                var windows = SuppressConfirmationService.Windows.Replay();
                using (windows.Connect()){
                    var window = application.CreateWindow(TemplateContext.View, null,true);
                    var objectView = application.CreateObjectView(viewType,typeof(SC));

                    window.SetView(objectView);

                    await windows.FirstAsync();
                }
            }
        }

        [Fact]
        public async Task Signal_When_DashboardView_with_SupressConfirmation_Enabled_ObjectView_changed(){
            using (var application = DefaultSuppressConfirmationModule().Application){
                var windows = SuppressConfirmationService.Windows.Replay();
                using (windows.Connect()){
                    var modelDashboardView = application.Model.NewModelDashboardView(typeof(SC));
                    var dashboardView = application.CreateDashboardView(application.CreateObjectSpace(), modelDashboardView.Id, true);
                    dashboardView.MockCreateControls();

                    var frame = await windows.Take(1);
                    frame.ShouldBeOfType<NestedFrame>();
                    frame = await windows.Take(1);
                    frame.ShouldBeOfType<NestedFrame>();
                }
            }
        }

        [Theory]
        [InlineData(typeof(ListView))]
        [InlineData(typeof(DetailView))]
        public  void Change_Modification_Handling_Mode(Type viewType){
            using (var application = DefaultSuppressConfirmationModule().Application){
                var windows = SuppressConfirmationService.Windows.Replay();
                using (windows.Connect()){
                    var window = application.CreateWindow(TemplateContext.View, null,true);
                    var objectView = application.CreateObjectView(viewType,typeof(SC));
                    objectView.CurrentObject = objectView.ObjectSpace.CreateObject(typeof(SC));
                    window.SetView(objectView);
                    objectView.ObjectSpace.CommitChanges();

                    window.GetController<ModificationsController>().ModificationsHandlingMode.ShouldBe((ModificationsHandlingMode) (-1));
                }
            }
        }


        private SuppressConfirmationModule DefaultSuppressConfirmationModule(){
            var application = new XafApplicationMock().Object;
            application.Title = "AutoCommitModule";
            var supressConfirmationModule = new SuppressConfirmationModule();
            supressConfirmationModule.AdditionalExportedTypes.AddRange(new[]{typeof(SC)});
            application.SetupDefaults(supressConfirmationModule);
            
            var modelClassSupressConfirmation = (IModelClassSupressConfirmation) application.Model.BOModel.GetClass(typeof(SC));
            modelClassSupressConfirmation.SupressConfirmation = true;
            return supressConfirmationModule;
        }
    }
}