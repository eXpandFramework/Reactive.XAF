using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.TestsLib;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.Reactive.Logger;
using Xpand.XAF.Modules.Reactive.Logger.Hub;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Tests.BOModel;


namespace Xpand.XAF.Modules.Reactive.Tests{
    [NonParallelizable]
    public class XafApplicationTests : BaseTest{
        [XpandTimeout]
        [TestCase(nameof(Platform.Win))]
        [TestCase(nameof(Platform.Web))]
        public async Task WhenFrameCreated(string platformName){
            var platform = GetPlatform(platformName);
            using (var application = DefaultReactiveModule(platform).Application){
                var frames = application.WhenFrameCreated().Replay();
                frames.Connect();

                var frame = application.CreateFrame(TemplateContext.View);
                var window = application.CreateWindow(TemplateContext.ApplicationWindow, new List<Controller>(), true);
                var nestedFrame = application.CreateNestedFrame(null, TemplateContext.ApplicationWindow);
                var popupWindow = application.CreatePopupWindow(TemplateContext.ApplicationWindow, null);

                (await frames.Take(1)).ShouldBe(frame);
                (await frames.Take(2)).ShouldBe(window);
                (await frames.Take(3)).ShouldBe(nestedFrame);
                (await frames.Take(4)).ShouldBe(popupWindow);
            }

            
        }

        [XpandTimeout]
        [TestCase(nameof(Platform.Win))]
        [TestCase(nameof(Platform.Web))]
        public async Task WhenWindowCreated(string platformName){
            var platform = GetPlatform(platformName);
            using (var application = DefaultReactiveModule(platform).Application){
                var windows = application.WhenWindowCreated().Replay();
                windows.Connect();

                var window = application.CreateWindow(TemplateContext.ApplicationWindow, new List<Controller>(), true);
                var popupWindow = application.CreatePopupWindow(TemplateContext.ApplicationWindow, null);
                (await windows.Take(1)).ShouldBe(window);
                (await windows.Take(2)).ShouldBe(popupWindow);
            }

            
        }

        [XpandTimeout]
        [TestCase(nameof(Platform.Win))]
        [TestCase(nameof(Platform.Web))]
        public async Task WhenPopupWindowCreated(string platformName){
            var platform = GetPlatform(platformName);
            using (var application = DefaultReactiveModule(platform).Application){
                var windows = application.WhenPopupWindowCreated().Replay();
                windows.Connect();

                var popupWindow = application.CreatePopupWindow(TemplateContext.ApplicationWindow, null);
                (await windows.Take(1)).ShouldBe(popupWindow);
            }

            
        }

        [XpandTimeout]
        [TestCase(nameof(Platform.Win))]
        [TestCase(nameof(Platform.Web))]
        public async Task WHen_NestedFrameCreated(string platformName){
            var platform = GetPlatform(platformName);
            using (var application = DefaultReactiveModule(platform).Application){
                var nestedFrames = application.WhenNestedFrameCreated().Replay();
                nestedFrames.Connect();

                var nestedFrame = application.CreateNestedFrame(null, TemplateContext.ApplicationWindow);
                (await nestedFrames.FirstAsync()).ShouldBe(nestedFrame);
            }

            
        }


        private static ReactiveModule DefaultReactiveModule(Platform platform=Platform.Win){
            var application = platform.NewApplication<ReactiveModule>();
            return application.AddModule<ReactiveModule>(typeof(R));
        }

        [Test]
        public void BufferUntilCompatibilityChecked(){
            var source = new Subject<int>();
            using (var application = DefaultReactiveModule().Application){
                var buffer = application.BufferUntilCompatibilityChecked(source).SubscribeReplay();
                source.OnNext(1);
                source.OnNext(2);
                buffer.Test().Items.Count.ShouldBe(0);

                application.CreateObjectSpace();

                source.OnNext(3);

            
                buffer.Test().Items.Count.ShouldBe(3);

            }

            
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void UnloadReactiveModule(){
            using (var application = Platform.Win.NewApplication<TestModule>()){
                var testModule = application.AddModule<TestModule>();
                application.Modules.OfType<ReactiveModule>().FirstOrDefault().ShouldBeNull();
                application.Modules.OfType<ReactiveLoggerModule>().FirstOrDefault().ShouldBeNull();
                application.Modules.OfType<ReactiveLoggerHubModule>().FirstOrDefault().ShouldBeNull();
                testModule.RequiredModuleTypes.Contains(typeof(ReactiveModule)).ShouldBeFalse();
                testModule.ModuleManager.Modules.Select(_ => _.GetType()).Contains(typeof(ReactiveModule)).ShouldBeFalse();
                testModule.ModuleManager.Modules.Select(_ => _.GetType()).Contains(typeof(ReactiveLoggerModule)).ShouldBeFalse();
                testModule.ModuleManager.Modules.Select(_ => _.GetType()).Contains(typeof(ReactiveLoggerHubModule)).ShouldBeFalse();
                ((IModelSources)application.Model).Modules.FirstOrDefault(_ => _ is ReactiveModule).ShouldBeNull();
                ((IModelSources)application.Model).Modules.FirstOrDefault(_ => _ is ReactiveLoggerModule).ShouldBeNull();
                ((IModelSources)application.Model).Modules.FirstOrDefault(_ => _ is ReactiveLoggerHubModule).ShouldBeNull();
            }
        }

    }
}