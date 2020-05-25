using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.TestsLib;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.MasterDetail;
using Xpand.XAF.Modules.SuppressConfirmation.Tests.BOModel;

namespace Xpand.XAF.Modules.SuppressConfirmation.Tests{
    [NonParallelizable]
    public class SuppressConfirmationTests : BaseTest{

        [XpandTest]
        [TestCase(typeof(ListView),nameof(Platform.Win))]
        [TestCase(typeof(DetailView),nameof(Platform.Win))]
        [TestCase(typeof(ListView),nameof(Platform.Web))]
        [TestCase(typeof(DetailView),nameof(Platform.Web))]
        public async Task Signal_When_Window_with_SupressConfirmation_Enabled_ObjectView_changed(Type viewType,string platformName){
            var platform = GetPlatform(platformName);
            using (var application = DefaultSuppressConfirmationModule(platform).Application){
                var windows = application.WhenSuppressConfirmationWindows().Replay();
                windows.Connect();
                var window = application.CreateWindow(TemplateContext.View, null, true);
                var objectView = application.NewObjectView(viewType, typeof(SC));
                window.SetView(objectView);

                await windows.FirstAsync();
            }

            
        }

        [XpandTest]
        [TestCase(nameof(Platform.Win))]
        [TestCase(nameof(Platform.Web))]
        public async Task Signal_When_DashboardView_with_SupressConfirmation_Enabled_ObjectView_changed(string platformName){
            var platform = GetPlatform(platformName);
            using (var application = DefaultSuppressConfirmationModule(platform).Application){
                var windows = application.WhenSuppressConfirmationWindows().Replay();
                windows.Connect();
                var modelDashboardView = application.Model.NewMasterDetailModelDashboardView(typeof(SC));
                var dashboardView = application.CreateDashboardView(application.CreateObjectSpace(), modelDashboardView.Id, true);
                dashboardView.MockCreateControls();

                var frame = await windows.Take(1);
                frame.ShouldBeOfType<NestedFrame>();
                frame = await windows.Take(1);
                frame.ShouldBeOfType<NestedFrame>();
            }

            
        }

        [XpandTest]
        [TestCase(typeof(ListView),nameof(Platform.Win))]
        [TestCase(typeof(DetailView),nameof(Platform.Win))]
        [TestCase(typeof(ListView),nameof(Platform.Web))]
        [TestCase(typeof(DetailView),nameof(Platform.Web))]
        public void Change_Modification_Handling_Mode(Type viewType,string platformName){
            var platform = GetPlatform(platformName);
            using (var application = DefaultSuppressConfirmationModule(platform).Application){
                var windows = application.WhenSuppressConfirmationWindows().Replay();
                windows.Connect();
                var window = application.CreateWindow(TemplateContext.View, null, true);
                var objectView = application.NewObjectView(viewType, typeof(SC));
                objectView.CurrentObject = objectView.ObjectSpace.CreateObject(typeof(SC));
                window.SetView(objectView);
                objectView.ObjectSpace.CommitChanges();

                window.GetController<ModificationsController>().ModificationsHandlingMode.ShouldBe((ModificationsHandlingMode) (-1));
                window.Close();
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