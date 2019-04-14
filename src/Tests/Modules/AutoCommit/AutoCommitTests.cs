using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using Shouldly;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Agnostic.Tests.Artifacts;
using Xpand.XAF.Agnostic.Tests.Modules.AutoCommit.BOModel;
using Xpand.XAF.Modules.AutoCommit;
using Xunit;

namespace Xpand.XAF.Agnostic.Tests.Modules.AutoCommit{
    [Collection(nameof(XafTypesInfo))]
    public class AutoCommitTests : BaseTest{

        [Fact]
        public async Task Signal_When_AutoCommit_Enabled_ObjectView_Created(){
            var application = DefaultAutoCommitModule().Application;
            
            var objectViews = AutoCommitService.ObjectViews.Replay();
            objectViews.Connect();

            var listView = application.CreateObjectView<ListView>(typeof(AC));
            var detailView = application.CreateObjectView<DetailView>(typeof(AC));

            (await objectViews.Take(1)).ShouldBe(listView);
            (await objectViews.Take(2)).ShouldBe(detailView);

            

        }

        [Fact]
        public void AutoCommit_When_object_view_closing(){
            var application = DefaultAutoCommitModule().Application;
            var detailView = application.CreateObjectView<DetailView>(typeof(AC));
            detailView.ObjectSpace.CreateObject<AC>();

            detailView.Close();

            application.CreateObjectSpace().FindObject<AC>(null).ShouldNotBeNull();

        }

        private AutoCommitModule DefaultAutoCommitModule(){
            var application = new XafApplicationMock().Object;

            var autoCommitModule = new AutoCommitModule();
            autoCommitModule.AdditionalExportedTypes.AddRange(new[]{typeof(AC)});
            application.SetupDefaults(autoCommitModule);
            
            var modelClassAutoCommit = (IModelClassAutoCommit) application.Model.BOModel.GetClass(typeof(AC));
            modelClassAutoCommit.AutoCommit = true;
            return autoCommitModule;
        }
    }
}