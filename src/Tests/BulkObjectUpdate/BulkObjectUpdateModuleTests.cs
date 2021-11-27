using System.Linq;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.CollectionSourceExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.BulkObjectUpdate.Tests.BOModel;
using Xpand.XAF.Modules.BulkObjectUpdate.Tests.Common;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.BulkObjectUpdate.Tests {
    public class BulkObjectUpdateModuleTests : CommonAppTest {
        private Window _window;
        private IModelBulkObjectUpdate _bulkObjectUpdate;
        private Frame _detailViewFrame;

        public override void Init() {
            base.Init();
            var objectSpace = Application.CreateObjectSpace();
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
            
            dialogController.AcceptAction.DoExecute();

            var asListView = _window.View.AsListView();
            var bou = asListView.CollectionSource.Objects().Cast<BOU>().First();
            bou.Name.ShouldBe("string");
        }

        [Test][Order(30)]
        public void Commit_The_Transaction() 
            => _window.View.ObjectSpace.IsModified.ShouldBeFalse();

    }
}
