using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Security.ClientServer;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Tests.Common;


namespace Xpand.XAF.Modules.Reactive.Tests{
    
    public class ObjectSpaceCreatedTests : ReactiveCommonTest {
        [XpandTest]
        [TestCase(nameof(Platform.Win))]
        public void WhenObjectSpaceCreated(string platformName){
            var platform = GetPlatform(platformName);
            using var application = DefaultReactiveModule(platform).Application;
            using var exiTest = application.WhenObjectSpaceCreated().Test();

            var objectSpace = application.CreateObjectSpace();

            exiTest.Items.Any(space => space==objectSpace).ShouldBeTrue();
        }
        [XpandTest]
        [TestCase(nameof(Platform.Win))]
        public void WhenNonSecuredObjectSpaceCreated(string platformName){
            var application = NewXafApplication();
            
            application.SetupSecurity();
            application.AddObjectSpaceProvider(new SecuredObjectSpaceProvider(
                (ISelectDataSecurityProvider)application.Security, new MemoryDataStoreProvider()));
            DefaultReactiveModule(application);
            
            using var exiTest = application.WhenProviderObjectSpaceCreated().TakeFirst().Select(space => space).Test();

            var nonsecuredObjectSpace = ((INonsecuredObjectSpaceProvider)application.ObjectSpaceProvider).CreateNonsecuredObjectSpace();

            exiTest.AwaitDone(Timeout).Items.Any(space => space==nonsecuredObjectSpace).ShouldBeTrue();
        }

    }

    public class XafApplicationTests : ReactiveCommonTest{
        [XpandTest]
        [TestCase(nameof(Platform.Win))]
        public async Task WhenFrameCreated(string platformName){
            var platform = GetPlatform(platformName);
            using var application = DefaultReactiveModule(platform).Application;
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

        [XpandTest]
        [TestCase(nameof(Platform.Win))]
        public async Task WhenWindowCreated(string platformName){
            var platform = GetPlatform(platformName);
            using var application = DefaultReactiveModule(platform).Application;
            var windows = application.WhenWindowCreated().Replay();
            windows.Connect();

            var window = application.CreateWindow(TemplateContext.ApplicationWindow, new List<Controller>(), true);
            var popupWindow = application.CreatePopupWindow(TemplateContext.ApplicationWindow, null);
            (await windows.Take(1)).ShouldBe(window);
            (await windows.Take(2)).ShouldBe(popupWindow);
        }

        [XpandTest]
        [TestCase(nameof(Platform.Win))]
        public async Task WhenPopupWindowCreated(string platformName){
            var platform = GetPlatform(platformName);
            using var application = DefaultReactiveModule(platform).Application;
            var windows = application.WhenPopupWindowCreated().Replay();
            windows.Connect();

            var popupWindow = application.CreatePopupWindow(TemplateContext.PopupWindow, null);
            (await windows.Take(1)).ShouldBe(popupWindow);
        }

        [XpandTest]
        [TestCase(nameof(Platform.Win))]
        public async Task WHen_NestedFrameCreated(string platformName){
            var platform = GetPlatform(platformName);
            using var application = DefaultReactiveModule(platform).Application;
            var nestedFrames = application.WhenNestedFrameCreated().Replay();
            nestedFrames.Connect();

            var nestedFrame = application.CreateNestedFrame(null, TemplateContext.ApplicationWindow);
            (await nestedFrames.TakeFirst()).ShouldBe(nestedFrame);
        }
        

        [Test]
        [XpandTest]
        public void BufferUntilCompatibilityChecked(){
            var source = new Subject<int>();
            using var application = DefaultReactiveModule().Application;
            var buffer = application.BufferUntilCompatibilityChecked(source).SubscribeReplay();
            source.OnNext(1);
            source.OnNext(2);
            buffer.Test().Items.Count.ShouldBe(0);

            application.CreateObjectSpace();

            source.OnNext(3);
                
            buffer.Test().Items.Count.ShouldBe(3);
        }

        
        [Test]
        [XpandTest()]
        public async Task Logon_with_user_key(){
            using var application = NewXafApplication();
            application.SetupSecurity();
            DefaultReactiveModule(application);
            var objectSpace = application.CreateObjectSpace();
            var policyUser = objectSpace.GetObjectsQuery<PermissionPolicyUser>().First();
            policyUser.SetPassword("test");
            objectSpace.CommitChanges();
                
            await application.LogonUser(policyUser.Oid).TakeFirst();
                
            SecuritySystem.CurrentUserId.ShouldBe(policyUser.Oid);
        }

        [XpandTest]
        [TestCase(nameof(Platform.Win))]
        public void WHen_Exiting(string platformName){
            var platform = GetPlatform(platformName);
            using var application = DefaultReactiveModule(platform).Application;
            var exiTest = application.WhenExiting().Test();

            application.Exit();

            exiTest.Items.Count.ShouldBe(1);
        }

    }
}