using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AppDomainToolkit;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Templates;
using DevExpress.ExpressApp.Templates.ActionContainers;
using DevExpress.ExpressApp.Win.Controls;
using DevExpress.ExpressApp.Win.SystemModule;
using DevExpress.XtraBars;
using Fasterflect;
using Moq;
using Shouldly;
using Tests.Artifacts;
using Tests.Modules.HideToolBar.BOModel;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.HideToolBar;
using Xunit;

namespace Tests.Modules.HideToolBar{
    [Collection(nameof(XafTypesInfo))]
    public class HideToolBarTests : BaseTest{

        [Theory]
        [InlineData(Platform.Web)]
        [InlineData(Platform.Win)]
        internal async Task Signal_When_frame_with_HideToolBar_Enabled_ListView_controls_created(Platform platform){
            await RemoteFuncAsync.InvokeAsync(Domain, platform, async p => {
                var application = DefaultHideToolBarModule(p).Application;
                var nestedFrames = application.HideToolBarNestedFrames().Replay();
                nestedFrames.Connect();
                var nestedFrame = application.CreateNestedFrame(null, TemplateContext.NestedFrame);
                nestedFrame.CreateTemplate();
                var detailView = application.CreateObjectView<DetailView>(typeof(HTBParent));
                nestedFrame.SetView(detailView);


                (await nestedFrames.Take(1)).ShouldBe(nestedFrame);
                return Unit.Default;

            });
        }


        private static Mock<IWindowTemplate> MockFrameTemplate(BarManager barManager=null){
            barManager = barManager ?? new BarManager();
            var holderMock = new Mock<IBarManagerHolder>();
            holderMock.SetupGet(holder => holder.BarManager).Returns(barManager);
            var templateMock = holderMock.As<IWindowTemplate>();
            templateMock.Setup(template => template.GetContainers()).Returns(new ActionContainerCollection());
            return templateMock;
        }

        [Theory]
        [InlineData(Platform.Web)]
        [InlineData(Platform.Win)]
        internal void Hide_Nested_ToolBar(Platform platform){
            var nestedFrame = new NestedFrame(platform.NewApplication(),TemplateContext.NestedFrame, null,new List<Controller>());
            nestedFrame.CreateTemplate();
            if (platform == Platform.Web){
                nestedFrame.Template.GetMock().As<ISupportActionsToolbarVisibility>()
                    .Setup(visibility => visibility.SetVisible(false));
            }
            nestedFrame.HideToolBar();

            if (platform==Platform.Win){
                ((IBarManagerHolder) nestedFrame.Template).BarManager.Bars.Any(bar => bar.Visible).ShouldBe(false);
            }
            else{
                nestedFrame.Template.GetMock().Verify();
            }
        }

        [Fact]
        internal void Hide_ToolbarVisibilityController(){
            var frame = new Frame(Platform.Win.NewApplication(),TemplateContext.ApplicationWindow);
            var frameTemplate = MockFrameTemplate();
            frame.SetFieldValue("template", frameTemplate.Object);
            var controller = new ToolbarVisibilityController();
            frame.RegisterController(controller);

            frame.HideToolBar();

            controller.Active[HideToolBarModule.CategoryName].ShouldBe(false);
        }


        private static HideToolBarModule DefaultHideToolBarModule(Platform platform){
            var application = platform.NewApplication();
            application.Title = "HideToolBarModule";
            var hideToolBarModule = new HideToolBarModule();
            hideToolBarModule.AdditionalExportedTypes.AddRange(new[]{typeof(HTBParent)});
            application.SetupDefaults(hideToolBarModule);
            
            var modelClassAutoCommit = (IModelClassHideToolBar) application.Model.BOModel.GetClass(typeof(HTBParent));
            modelClassAutoCommit.HideToolBar = true;
            return hideToolBarModule;
        }
    }

    public class ApplicationMock<T>:Mock<T> where T : XafApplication{
        public ApplicationMock(){
            CallBase = true;
        }

        public sealed override bool CallBase{
            get => base.CallBase;
            set => base.CallBase = value;
        }
    }
}