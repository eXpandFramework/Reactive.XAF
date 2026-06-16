using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.SystemModule;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.CollectionSourceExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Blazor;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.BulkObjectUpdate.Tests.BOModel;
using Xpand.XAF.Modules.BulkObjectUpdate.Tests.Common;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.BulkObjectUpdate.Tests {
    class MyClass:BlazorCommonTest {
        protected IObservable<Unit> StartBulkObjectUpdateTest(Func<BlazorApplication, IObservable<Unit>> test,Func<WebHostBuilderContext, TestStartup> startupFactory=null,TimeSpan? timeOut=null) 
            => StartTest(test,configureWebHostBuilder:ConfigureWebHostBuilder(),startupFactory:context => startupFactory?.Invoke(context),timeOut:timeOut,configureServices:ConfigureServices);
        
        private Action<IWebHostBuilder> ConfigureWebHostBuilder() 
            => builder => builder.UseSetting(WebHostDefaults.HostingStartupAssembliesKey, GetType().Assembly.GetName().Name);
        
        private void ConfigureServices(IServiceCollection services) {
            
        }

        [Test]
        public async Task MethodName() {
            await StartBulkObjectUpdateTest(application => application.WhenLoggedOn("Admin").TakeFirst()
                .SelectMany(_ => application.WhenViewOnFrame().TakeFirst().ToUnit()));
        }

    }
    public class BulkObjectUpdateModuleTests : CommonAppTest {
        private Window _window;
        private IModelBulkObjectUpdate _bulkObjectUpdate;
        private Frame _detailViewFrame;

        public override void Init() {
            ReactiveModuleBase.Scheduler=TestScheduler;
            TestScheduler.AdvanceTimeBy(2.Seconds());
            base.Init();
            var objectSpace = Application.CreateObjectSpace<BOU>();
            objectSpace.CreateObject<BOU>();
            objectSpace.CommitChanges();
            _bulkObjectUpdate = Application.Model.ToReactiveModule<IModelReactiveModulesBulkObjectUpdate>().BulkObjectUpdate;
            var rule1 = _bulkObjectUpdate.Rules.AddNode<IModelBulkObjectUpdateRule>("1");
            rule1.ListView = Application.Model.BOModel.GetClass(typeof(BOU)).DefaultListView;
            var rule2 = _bulkObjectUpdate.Rules.AddNode<IModelBulkObjectUpdateRule>("2");
            rule2.ListView = Application.Model.BOModel.GetClass(typeof(BOU)).DefaultListView;
            rule2.DetailView = Application.Model.BOModel.GetClass(typeof(BOU2)).DefaultDetailView;
            
            _window = Application.CreateViewWindow();
            _window.SetView(Application.NewView<ListView>(typeof(BOU)));
            TestScheduler.AdvanceTimeBy(2.Seconds());
        }

        [Test][Order(0)]
        public void BulkUpdate_Items_Contain_Model_Rules() {
            
            var action = _window.Action(nameof(BulkObjectUpdateService.BulkUpdate)) as SingleChoiceAction;
            
            action.ShouldNotBeNull();
            action.Items.Count.ShouldBe(2);
            action.Items.First().Caption.ShouldBe(_bulkObjectUpdate.Rules.First().Caption);
            action.Items.Last().Caption.ShouldBe(_bulkObjectUpdate.Rules.Last().Caption);
        }

        [Test][Order(10)]
        public void Shows_Selected_ActionItem_DetailView() {
            var action = _window.Action(nameof(BulkObjectUpdateService.BulkUpdate)) as SingleChoiceAction;
            using var testObserver = _window.Application.WhenViewOnFrame().WhenFrame(ViewType.DetailView).Test();
            
            action.DoExecute(space => space.GetObjectsQuery<BOU>().ToArray());
            
            testObserver.ItemCount.ShouldBe(1);
            _detailViewFrame = testObserver.Items.First();
        }

        [Test][Order(20)]
        public void Updates_Selected_ListView_Objects() {
            var dialogController = _detailViewFrame.GetController<DialogController>();
            ((BOU)dialogController.Frame.View.CurrentObject).Name = "string";
            var listView = _window.View.AsListView();
            listView.Editor.GetMock().Setup(editor => editor.GetSelectedObjects())
                .Returns(() => listView.CollectionSource.Objects().ToList());
            dialogController.AcceptAction.DoExecute();

            var asListView = listView;
            var bou = asListView.CollectionSource.Objects().Cast<BOU>().First();
            bou.Name.ShouldBe("string");
        }

        [Test][Order(30)]
        public void Commit_The_Transaction() 
            => _window.View.ObjectSpace.IsModified.ShouldBeFalse();

    }
}
