using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.TestsLib;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.AutoCommit.Tests.BOModel;


namespace Xpand.XAF.Modules.AutoCommit.Tests{
    [NonParallelizable]
    public class AutoCommitTests : BaseTest{

        [XpandTest]
        [TestCase(nameof(Platform.Win))]
        [TestCase(nameof(Platform.Web))]
        public async Task Signal_When_AutoCommit_Enabled_ObjectView_Created(string platformName){
            using (var application = DefaultAutoCommitModule(platformName,nameof(Signal_When_AutoCommit_Enabled_ObjectView_Created)).Application){

                var objectViews = application.WhenAutoCommitObjectViewCreated().SubscribeReplay();
                
                var listView = application.CreateObjectView<ListView>(typeof(AC));
                var detailView = application.CreateObjectView<DetailView>(typeof(AC));

                (await objectViews.Take(1).WithTimeOut()).ShouldBe(listView);
                (await objectViews.Take(2).WithTimeOut()).ShouldBe(detailView);
                
            }
        }
        [XpandTest]
        [TestCase(nameof(Platform.Win))]
        [TestCase(nameof(Platform.Web))]
        public void AutoCommit_When_object_view_closing(string platformName){
            
            using (var application = DefaultAutoCommitModule(platformName, nameof(AutoCommit_When_object_view_closing)).Application){
                var detailView = application.CreateObjectView<DetailView>(typeof(AC));
                detailView.ObjectSpace.CreateObject<AC>();

                detailView.Close();

                application.CreateObjectSpace().FindObject<AC>(null).ShouldNotBeNull();
            }
        }

        private  AutoCommitModule DefaultAutoCommitModule(string platformName,string title){
            var platform = GetPlatform(platformName);
            var autoCommitModule = new AutoCommitModule();
            var application = platform.NewApplication<AutoCommitModule>();
            application.Title = title;
            autoCommitModule.AdditionalExportedTypes.AddRange(new[]{typeof(AC)});
            application.SetupDefaults(autoCommitModule);
            
            var modelClassAutoCommit = (IModelClassAutoCommit) application.Model.BOModel.GetClass(typeof(AC));
            modelClassAutoCommit.AutoCommit = true;
            application.Logon();
            return autoCommitModule;
        }
    }
}