using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Shouldly;
using TestsLib;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.MasterDetail;
using Xpand.XAF.Modules.SuppressConfirmation.Tests.BOModel;
using Xunit;

namespace Xpand.XAF.Modules.SuppressConfirmation.Tests{
    [Collection(nameof(SuppressConfirmationModule))]
    public class SuppressConfirmationTests : BaseTest{

        [Theory]
        [InlineData(typeof(ListView),Platform.Win)]
        [InlineData(typeof(DetailView),Platform.Win)]
        [InlineData(typeof(ListView),Platform.Web)]
        [InlineData(typeof(DetailView),Platform.Web)]
        internal async Task Signal_When_Window_with_SupressConfirmation_Enabled_ObjectView_changed(Type viewType,Platform platform){
            using (var application = DefaultSuppressConfirmationModule(platform).Application){
                var windows = application.WhenSuppressConfirmationWindows().Replay();
                windows.Connect();
                var window = application.CreateWindow(TemplateContext.View, null, true);
                var objectView = application.CreateObjectView(viewType, typeof(SC));
                window.SetView(objectView);

                await windows.FirstAsync();
            }

            
        }

        [Theory]
        [InlineData(Platform.Win)]
        [InlineData(Platform.Web)]
        internal async Task Signal_When_DashboardView_with_SupressConfirmation_Enabled_ObjectView_changed(Platform platform){
            using (var application = DefaultSuppressConfirmationModule(platform).Application){
                var windows = application.WhenSuppressConfirmationWindows().Replay();
                windows.Connect();
                var modelDashboardView = application.Model.NewModelDashboardView(typeof(SC));
                var dashboardView = application.CreateDashboardView(application.CreateObjectSpace(), modelDashboardView.Id, true);
                dashboardView.MockCreateControls();

                var frame = await windows.Take(1);
                frame.ShouldBeOfType<NestedFrame>();
                frame = await windows.Take(1);
                frame.ShouldBeOfType<NestedFrame>();
            }

            
        }

        [Theory]
        [InlineData(typeof(ListView),Platform.Win)]
        [InlineData(typeof(DetailView),Platform.Win)]
        [InlineData(typeof(ListView),Platform.Web)]
        [InlineData(typeof(DetailView),Platform.Web)]
        internal void Change_Modification_Handling_Mode(Type viewType,Platform platform){
            using (var application = DefaultSuppressConfirmationModule(platform).Application){
                var windows = application.WhenSuppressConfirmationWindows().Replay();
                windows.Connect();
                var window = application.CreateWindow(TemplateContext.View, null, true);
                var objectView = application.CreateObjectView(viewType, typeof(SC));
                objectView.CurrentObject = objectView.ObjectSpace.CreateObject(typeof(SC));
                window.SetView(objectView);
                objectView.ObjectSpace.CommitChanges();

                window.GetController<ModificationsController>().ModificationsHandlingMode.ShouldBe((ModificationsHandlingMode) (-1));
            }
        }


        private static SuppressConfirmationModule DefaultSuppressConfirmationModule(Platform platform){
            var application = platform.NewApplication<SuppressConfirmationModule>();
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