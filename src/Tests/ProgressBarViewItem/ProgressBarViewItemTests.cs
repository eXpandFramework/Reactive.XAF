using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.Web;
using DevExpress.XtraEditors;
using Fasterflect;
using Moq;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.TestsLib.Net461;
using Xpand.XAF.Modules.ProgressBarViewItem.Tests.BOModel;

namespace Xpand.XAF.Modules.ProgressBarViewItem.Tests{
    [NonParallelizable]
    public class ProgressBarViewItemTests : BaseTest{
        [SetUp]
        public void SetUp(){
            typeof(ProgressBarViewItemBase).SetFieldValue("_platform", null);
        }

        [XpandTest]
        [TestCase(nameof(Platform.Web))]
        [TestCase(nameof(Platform.Win))]
        public void Editor_registration(string platformName){
            var platform = GetPlatform(platformName);
            var defaultProgressBarViewItemModule = DefaultProgressBarViewItemModule(platform);

            defaultProgressBarViewItemModule.Application.Model.ViewItems.Select(item => $"IModel{item.Name}")
                .FirstOrDefault(s => s == nameof(IModelProgressBarViewItem)).ShouldNotBeNull();
        }

        [XpandTest]
        [TestCase(nameof(Platform.Web), typeof(ASPxProgressBar))]
        [TestCase(nameof(Platform.Win), typeof(ProgressBarControl))]
        public void ProgressBarControl_Type(string platformName, Type progressBarType){
            var platform = GetPlatform(platformName);
            var application = DefaultProgressBarViewItemModule(platform).Application;
            var objectView = application.NewObjectView<DetailView>(typeof(PBVI));

            if (platform == Platform.Win){
                var unused = new ProgressBarControl();
            }
            else if (platform == Platform.Web){
                var unused = new ASPxProgressBar();
            }

            var progressBarViewItem = new Mock<ProgressBarViewItemBase>(Mock.Of<IModelProgressBarViewItem>(), GetType())
                {CallBase = true}.Object;
            progressBarViewItem.Setup(null, application);
            progressBarViewItem.View = objectView;
            progressBarViewItem.CreateControl();

            progressBarViewItem.Control.ShouldBeOfType(progressBarType);
        }

        private static ProgressBarViewItemModule DefaultProgressBarViewItemModule(Platform platform){
            return platform.NewApplication<ProgressBarViewItemModule>()
                .AddModule<ProgressBarViewItemModule>(typeof(PBVI));
        }

        [XpandTest]
        [TestCase(nameof(Platform.Win))]
        [Apartment(ApartmentState.STA)]
        public async Task Can_Observe_an_asynchronous_sequential_percentage_sequence(string platformName){
            var platform = GetPlatform(platformName);
            var signal = Observable.Interval(TimeSpan.FromMilliseconds(10))
                .Select(l => (decimal) l)
                .Take(2).Do(obj => { }, () => { });

            using var newApplication = DefaultProgressBarViewItemModule(platform).Application;
            if (platform == Platform.Win){
                var unused = new ProgressBarControl();
                var progressBarViewItem =
                    new Mock<ProgressBarViewItemBase>(Mock.Of<IModelProgressBarViewItem>(), GetType())
                        {CallBase = true}.Object;
                progressBarViewItem.Setup(null, newApplication);
                progressBarViewItem.CreateControl();
                progressBarViewItem.Start();
                var progressBarControl = (ProgressBarControl) progressBarViewItem.Control;
                var whenPositionChanged = Observable
                    .FromEventPattern<EventHandler, EventArgs>(h => progressBarControl.PositionChanged += h,
                        h => progressBarControl.PositionChanged -= h, ImmediateScheduler.Instance).Replay();
                whenPositionChanged.Connect();

                await signal.Do(progressBarViewItem).Timeout(Timeout).ToTaskWithoutConfigureAwait();

                await whenPositionChanged.Take(2).Timeout(Timeout).ToTaskWithoutConfigureAwait();
                progressBarViewItem.Position.ShouldBe(0);
            }
        }
    }
}