using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.Web;
using DevExpress.XtraEditors;
using Moq;
using Shouldly;
using Tests.Artifacts;
using Tests.Modules.ProgressBarViewItem.BOModel;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.ProgressBarViewItem;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Modules.ProgressBarViewItem{
    [Collection(nameof(XafTypesInfo))]
    public class ProgressBarViewItemTests : BaseTest{
        public ProgressBarViewItemTests(ITestOutputHelper output) : base(output){
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
            progressBarViewItem.View=objectView;
            progressBarViewItem.CreateControl();

            progressBarViewItem.Control.ShouldBeOfType(progressBarType);
        }

        private static ProgressBarViewItemModule DefaultProgressBarViewItemModule(Platform platform){
            platform.Set(typeof(ProgressBarViewItemBase));
            return platform.NewApplication().AddModule<ProgressBarViewItemModule>(typeof(PBVI));
        }


        [Theory]
        [InlineData(Platform.Win)]
        internal async Task Can_Observe_an_asynchronous_sequencial_percentance_sequence(Platform platform){
            platform.Set(typeof(ProgressBarViewItemBase));
            var signal = Observable.Interval(TimeSpan.FromMilliseconds(10))
                .Select(l => (decimal)l)
                .Take(100);
            
            if (platform==Platform.Win){
                var unused = new ProgressBarControl();

                var progressBarViewItem = new Mock<ProgressBarViewItemBase>(Mock.Of<IModelProgressBarViewItem>(), GetType()){CallBase = true}.Object;
                progressBarViewItem.CreateControl();
                progressBarViewItem.Start();
                var progressBarControl = (ProgressBarControl) progressBarViewItem.Control;
                var whenPositionChanged = Observable.FromEventPattern<EventHandler,EventArgs>(h => progressBarControl.PositionChanged+=h,h => progressBarControl.PositionChanged-=h).Replay();
                whenPositionChanged.Connect();

                await signal.Do(progressBarViewItem);

                await whenPositionChanged.Take(100).WithTimeOut();
                progressBarViewItem.Position.ShouldBe(0);
            }
            else{
                var progressBarViewItem = new Mock<ProgressBarViewItemBase>(Mock.Of<IModelProgressBarViewItem>(), GetType()){CallBase = true}.Object;
                progressBarViewItem.CreateControl();
                progressBarViewItem.Start();
                

            }

            

            
            
        }


    }

}