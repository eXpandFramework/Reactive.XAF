using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.Web;
using DevExpress.XtraEditors;
using Fasterflect;
using Moq;
using Shouldly;
using TestsLib;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.ProgressBarViewItem.Tests.BOModel;
using Xunit;
using Xunit.Abstractions;

namespace Xpand.XAF.Modules.ProgressBarViewItem.Tests{
    [Collection(nameof(ProgressBarViewItemModule))]
    public class ProgressBarViewItemTests : BaseTest{
        public ProgressBarViewItemTests() {
            typeof(ProgressBarViewItemBase).SetFieldValue("_platform", null);
        }

        [Theory]
        [InlineData(Platform.Web)]
        [InlineData(Platform.Win)]
        internal void Editor_registration(Platform platform){

            var defaultProgressBarViewItemModule = DefaultProgressBarViewItemModule(platform);

            defaultProgressBarViewItemModule.Application.Model.ViewItems.Select(item => $"IModel{item.Name}")
                .FirstOrDefault(s => s == nameof(IModelProgressBarViewItem)).ShouldNotBeNull();

        }

        [Theory()]
        [InlineData(Platform.Web,typeof(ASPxProgressBar))]
        [InlineData(Platform.Win,typeof(ProgressBarControl))]
        internal void ProgressBarControl_Type(Platform platform,Type progressBarType){
            var application = DefaultProgressBarViewItemModule(platform).Application;
            var objectView = application.CreateObjectView<DetailView>(typeof(PBVI));
            
            if (platform == Platform.Win){
                var unused = new ProgressBarControl();
            }
            else if (platform == Platform.Web){
                var unused = new ASPxProgressBar();
            }
            
            var progressBarViewItem = new Mock<ProgressBarViewItemBase>(Mock.Of<IModelProgressBarViewItem>(), GetType()){CallBase = true}.Object;
            progressBarViewItem.Setup(null,application);
            progressBarViewItem.View=objectView;
            progressBarViewItem.CreateControl();

            progressBarViewItem.Control.ShouldBeOfType(progressBarType);
        }

        private static ProgressBarViewItemModule DefaultProgressBarViewItemModule(Platform platform){
            return platform.NewApplication<ProgressBarViewItemModule>().AddModule<ProgressBarViewItemModule>(typeof(PBVI));
        }



        [Theory]
        [InlineData(Platform.Win)]
        internal async Task Can_Observe_an_asynchronous_sequencial_percentance_sequence(Platform platform){
            var signal = Observable.Interval(TimeSpan.FromMilliseconds(10))
                .Select(l => (decimal)l)
                .Take(100);

            var newApplication = platform.NewApplication<ProgressBarViewItemModule>();
            newApplication.SetupDefaults();
            if (platform==Platform.Win){
                var unused = new ProgressBarControl();
                var progressBarViewItem = new Mock<ProgressBarViewItemBase>(Mock.Of<IModelProgressBarViewItem>(), GetType()){CallBase = true}.Object;
                progressBarViewItem.Setup(null,newApplication);
                progressBarViewItem.CreateControl();
                progressBarViewItem.Start();
                var progressBarControl = (ProgressBarControl) progressBarViewItem.Control;
                var whenPositionChanged = Observable.FromEventPattern<EventHandler,EventArgs>(h => progressBarControl.PositionChanged+=h,h => progressBarControl.PositionChanged-=h).Replay();
                whenPositionChanged.Connect();

                await signal.Do(progressBarViewItem);

                await whenPositionChanged.Take(100).WithTimeOut();
                progressBarViewItem.Position.ShouldBe(0);
            }
        }


    }

}