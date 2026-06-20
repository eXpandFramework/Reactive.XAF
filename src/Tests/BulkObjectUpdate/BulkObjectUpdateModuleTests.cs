using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.SystemModule;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.CollectionSourceExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.BulkObjectUpdate.Tests.BOModel;
using Xpand.XAF.Modules.BulkObjectUpdate.Tests.Common;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.BulkObjectUpdate.Tests {
    public class BulkObjectUpdateModuleTests:BulkObjectUpdateModuleTestsBaseTest {
        
        [Test]
        public async Task Run() 
            => await StartBulkObjectUpdateTest(application => application.WhenFirstFrame()
                .Merge(SetupModel(application)).Take(1)
                .Do(frame => frame.View.ObjectSpace.CreateObject<BOU>().CommitChanges())
                .Select(_ => application.Model.ToReactiveModule<IModelReactiveModulesBulkObjectUpdate>().BulkObjectUpdate)
                .SelectMany(update => application.Navigate(typeof(BOU))
                    .SelectMany(frame => frame.AssertListViewHasObject<BOU>())
                    .SelectMany(frame => frame.View.Objects().ToNowObservable().SelectMany(obj => frame.View.ToListView().SelectObject(obj).To(frame)))
                    .SelectMany(frame =>BulkUpdate_Items_Contain_Model_Rules(frame,update))
                    .Do(frame => {
                        var itemDetailView = Shows_Selected_ActionItem_DetailView(frame);
                        Updates_Selected_ListView_Objects(itemDetailView,frame );
                        Commit_The_Transaction(frame);
                    })).Take(1).ToUnit());

        private static IObservable<Frame> SetupModel(BlazorApplication application){
            return application.WhenSetupComplete().Do(_ => {
                var bulkObjectUpdate = application.Model.ToReactiveModule<IModelReactiveModulesBulkObjectUpdate>().BulkObjectUpdate;
                var rule1 = bulkObjectUpdate.Rules.AddNode<IModelBulkObjectUpdateRule>("1");
                rule1.Caption = rule1.Id();
                rule1.ListView = application.Model.BOModel.GetClass(typeof(BOU)).DefaultListView;
                var rule2 = bulkObjectUpdate.Rules.AddNode<IModelBulkObjectUpdateRule>("2");
                rule2.ListView = application.Model.BOModel.GetClass(typeof(BOU)).DefaultListView;
                rule2.DetailView = application.Model.BOModel.GetClass(typeof(BOU2)).DefaultDetailView;
                rule2.Caption = rule2.Id();
            }).To<Frame>().IgnoreElements();
        }

        IObservable<Frame> BulkUpdate_Items_Contain_Model_Rules(Frame frame,IModelBulkObjectUpdate bulkObjectUpdate) 
            => frame.AssertSingleChoiceAction(nameof(BulkObjectUpdateService.BulkUpdate), action => {
                action.Items.First().Caption.ShouldBe(bulkObjectUpdate.Rules.First().Caption);
                action.Items.Last().Caption.ShouldBe(bulkObjectUpdate.Rules.Last().Caption);
                return 2;
            }).To(frame);

        Frame Shows_Selected_ActionItem_DetailView(Frame frame) {
            var action = frame.Action(nameof(BulkObjectUpdateService.BulkUpdate)) as SingleChoiceAction;
            using var testObserver = frame.Application.WhenViewOnFrame().WhenFrame(ViewType.DetailView).Test();
            
            action.DoExecute(space => space.GetObjectsQuery<BOU>().ToArray());
            
            testObserver.ItemCount.ShouldBe(1);
            return testObserver.Items.First();
        }

        void Updates_Selected_ListView_Objects(Frame detailViewFrame, Frame frame) {
            var dialogController = detailViewFrame.GetController<DialogController>();
            ((BOU)dialogController.Frame.View.CurrentObject).Name = "string";
            var listView = frame.View.AsListView();
            dialogController.AcceptAction.DoExecute();

            
            var bou =  listView.CollectionSource.Objects().Cast<BOU>().First();
            bou.Name.ShouldBe("string");
        }

        void Commit_The_Transaction(Frame frame) 
            => frame.View.ObjectSpace.IsModified.ShouldBeFalse();
    }
}
