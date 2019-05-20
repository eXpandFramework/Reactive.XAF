using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using Shouldly;
using Tests.Artifacts;
using Tests.Modules.Reactive.BOModel;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xunit;

namespace Tests.Modules.Reactive{
    [Collection(nameof(XafTypesInfo))]
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
            application.Title = "ReactiveModule";
            var reactiveModule = new ReactiveModule();
            reactiveModule.AdditionalExportedTypes.AddRange(new[]{typeof(R)});
            application.SetupDefaults(reactiveModule);
            return reactiveModule;
        }
    }
}