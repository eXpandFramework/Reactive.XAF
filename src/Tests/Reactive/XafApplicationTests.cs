using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using Shouldly;
using TestsLib;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;
using Xunit;

namespace Xpand.XAF.Modules.Reactive.Tests{
    [Collection(nameof(ReactiveModule))]
    public class XafApplicationTests : BaseTest{
        [Theory]
        [InlineData(Platform.Win)]
        [InlineData(Platform.Web)]
        internal async Task WhenFrameCreated(Platform platform){
            var application = DefaultReactiveModule(platform).Application;
            var frames = application.WhenFrameCreated().Replay();
            frames.Connect();

            var frame = application.CreateFrame(TemplateContext.View);
            var window = application.CreateWindow(TemplateContext.ApplicationWindow, new List<Controller>(), true);
            var nestedFrame = application.CreateNestedFrame(null,TemplateContext.ApplicationWindow);
            var popupWindow = application.CreatePopupWindow(TemplateContext.ApplicationWindow, null);
                
            (await frames.Take(1)).ShouldBe(frame);
            (await frames.Take(2)).ShouldBe(window);
            (await frames.Take(3)).ShouldBe(nestedFrame);
            (await frames.Take(4)).ShouldBe(popupWindow);
        }

        [Theory]
        [InlineData(Platform.Win)]
        [InlineData(Platform.Web)]
        internal async Task WhenWindowCreated(Platform platform){
            var application = DefaultReactiveModule(platform).Application;
            var windows = application.WhenWindowCreated().Replay();
            windows.Connect();

            var window = application.CreateWindow(TemplateContext.ApplicationWindow, new List<Controller>(),true);
            var popupWindow = application.CreatePopupWindow(TemplateContext.ApplicationWindow, null);

            (await windows.Take(1)).ShouldBe(window);
            (await windows.Take(2)).ShouldBe(popupWindow);
        }

        [Theory]
        [InlineData(Platform.Win)]
        [InlineData(Platform.Web)]
        internal async Task WhenPopupWindowCreated(Platform platform){
            var application = DefaultReactiveModule(platform).Application;
            var windows = application.WhenPopupWindowCreated().Replay();
            windows.Connect();

            var popupWindow = application.CreatePopupWindow(TemplateContext.ApplicationWindow, null);

            (await windows.Take(1)).ShouldBe(popupWindow);
        }

        [Theory]
        [InlineData(Platform.Win)]
        [InlineData(Platform.Web)]
        internal async Task WHen_NestedFrameCreated(Platform platform){
            var application = DefaultReactiveModule(platform).Application;
            var nestedFrames = application.WhenNestedFrameCreated().Replay();
            nestedFrames.Connect();

            var nestedFrame = application.CreateNestedFrame(null,TemplateContext.ApplicationWindow);

            (await nestedFrames.FirstAsync()).ShouldBe(nestedFrame);
        }


        private static ReactiveModule DefaultReactiveModule(Platform platform){
            var application = platform.NewApplication();
            return application.AddModule<ReactiveModule>(typeof(R));
        }

        [Fact]
        public void BufferUntilCompatibilityChecked(){
            var source = new Subject<int>();
            var application = DefaultReactiveModule(Platform.Win).Application;
            var buffer = application.BufferUntilCompatibilityChecked(source).SubscribeReplay();
            source.OnNext(1);
            source.OnNext(2);
            buffer.Test().Items.Count.ShouldBe(0);

            application.CreateObjectSpace();
            source.OnNext(3);

            
            buffer.Test().Items.Count.ShouldBe(3);

        }

    }
}