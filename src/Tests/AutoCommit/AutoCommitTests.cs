using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.AutoCommit.Tests.BOModel;


namespace Xpand.XAF.Modules.AutoCommit.Tests{
    [NonParallelizable]
    public class AutoCommitTests : BaseTest{

        [XpandTest]
        [Test()]
        public async Task Signal_When_AutoCommit_Enabled_ObjectView_Created(){
            using var application = DefaultAutoCommitModule(Platform.Win,nameof(Signal_When_AutoCommit_Enabled_ObjectView_Created)).Application;
            var objectViews = application.WhenAutoCommitObjectViewCreated().SubscribeReplay();
                
            var listView = application.NewObjectView<ListView>(typeof(AC));
            var detailView = application.NewObjectView<DetailView>(typeof(AC));

            (await objectViews.Take(1).WithTimeOut()).ShouldBe(listView);
            (await objectViews.Take(2).WithTimeOut()).ShouldBe(detailView);
        }
        [XpandTest]
        [Test]
        public void AutoCommit_When_object_view_closing(){
            using var application = DefaultAutoCommitModule(Platform.Win, nameof(AutoCommit_When_object_view_closing)).Application;
            var detailView = application.NewObjectView<DetailView>(typeof(AC));
            detailView.ObjectSpace.CreateObject<AC>();

            detailView.Close();

            application.CreateObjectSpace().FindObject<AC>(null).ShouldNotBeNull();
        }

        private  AutoCommitModule DefaultAutoCommitModule(Platform platformName,string title){
            var platform = GetPlatform(platformName.ToString());
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