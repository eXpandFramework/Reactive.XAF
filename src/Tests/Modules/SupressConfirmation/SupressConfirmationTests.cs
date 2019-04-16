using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Shouldly;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Agnostic.Tests.Artifacts;
using Xpand.XAF.Agnostic.Tests.Modules.SupressConfirmation.BOModel;
using Xpand.XAF.Modules.SupressConfirmation;
using Xunit;

namespace Xpand.XAF.Agnostic.Tests.Modules.SupressConfirmation{
    [Xunit.Collection(nameof(XafTypesInfo))]
    public class SupressConfirmationTests : BaseTest{

        [Theory]
        [InlineData(typeof(ListView))]
        [InlineData(typeof(DetailView))]
        public async Task Signal_When_Windows_with_SupressConfirmation_Enabled_ObjectView_changed(Type viewType){
            using (var application = DefaultAutoCommitModule().Application){
                var windows = SupressConfirmationService.Windows.Replay();
                using (windows.Connect()){
                    var window = application.CreateWindow(TemplateContext.View, null,true);
                    var objectView = application.CreateObjectView(viewType,typeof(SC));

                    window.SetView(objectView);

                    await windows.FirstAsync();
                }
            }
        }

        [Theory]
        [InlineData(typeof(ListView))]
        [InlineData(typeof(DetailView))]
        public  void Change_Modification_Handling_Mode(Type viewType){
            using (var application = DefaultAutoCommitModule().Application){
                var windows = SupressConfirmationService.Windows.Replay();
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


        private SupressConfirmationModule DefaultAutoCommitModule(){
            var application = new XafApplicationMock().Object;
            application.Title = "AutoCommitModule";
            var supressConfirmationModule = new SupressConfirmationModule();
            supressConfirmationModule.AdditionalExportedTypes.AddRange(new[]{typeof(SC)});
            application.SetupDefaults(supressConfirmationModule);
            
            var modelClassSupressConfirmation = (IModelClassSupressConfirmation) application.Model.BOModel.GetClass(typeof(SC));
            modelClassSupressConfirmation.SupressConfirmation = true;
            return supressConfirmationModule;
        }
    }
}