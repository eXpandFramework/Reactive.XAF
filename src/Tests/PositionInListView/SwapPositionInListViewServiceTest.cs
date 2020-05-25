using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.Frame;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.TestsLib;
using Xpand.TestsLib.Attributes;
using Xpand.XAF.Modules.PositionInListview;
using Xpand.XAF.Modules.PositionInListView.Tests.BOModel;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.PositionInListView.Tests{
	public class SwapPositionInListViewServiceTest : PositionInListViewBaseTest{
		[Test]
		[XpandTest]
		public void Move_object_actions_are_active_only_for_model_views(){
			using (var applicatin = PositionInListViewModuleModule().Application){
				var modelPositionInListView = applicatin.Model.ToReactiveModule<IModelReactiveModulesPositionInListView>()
					.PositionInListView;
				var listViewItem = modelPositionInListView.ListViewItems.AddNode<IModelPositionInListViewListViewItem>();
				listViewItem.ListView = applicatin.Model.BOModel.GetClass(typeof(PIL)).DefaultListView;
				var viewWindow = applicatin.CreateViewWindow();

				viewWindow.SetView(applicatin.NewView(listViewItem.ListView));

				var moduleAction = viewWindow.Action<PositionInListViewModule>();
				moduleAction.MoveObjectUp().Active.ResultValue.ShouldBeTrue();
				moduleAction.MoveObjectDown().Active.ResultValue.ShouldBeTrue();
			}
		}

		[TestCase(nameof(SwapPositionInListViewService.MoveObjectUp))]
		[TestCase(nameof(SwapPositionInListViewService.MoveObjectDown))]
		[XpandTest]
		public void Move_object_action_is_disabled_when_on_edge(string actionId){
			using (var applicatin = PositionInListViewModuleModule().Application){
				var modelPositionInListView = applicatin.Model.ToReactiveModule<IModelReactiveModulesPositionInListView>()
					.PositionInListView;
				var listViewItem = modelPositionInListView.ListViewItems.AddNode<IModelPositionInListViewListViewItem>();
				listViewItem.ListView = applicatin.Model.BOModel.GetClass(typeof(PIL)).DefaultListView;
				var viewWindow = applicatin.CreateViewWindow();
				var objectSpace = applicatin.CreateObjectSpace();
				var pil1 = objectSpace.CreateObject<PIL>();
				pil1.Name = "a";
				var pil2 = objectSpace.CreateObject<PIL>();
				pil2.Name = "b";
				objectSpace.CommitChanges();
				var view = applicatin.NewView(listViewItem.ListView,
					space => new[]
						{space.GetObject(actionId == nameof(SwapPositionInListViewService.MoveObjectUp) ? pil1 : pil2)});
				viewWindow.SetView(view);


				view.AsObjectView().OnSelectionChanged();


				viewWindow.Action(actionId).Enabled[SwapPositionInListViewService.EdgeContext].ShouldBeFalse();
			}
		}

		[TestCase(nameof(SwapPositionInListViewService.MoveObjectUp), 3)]
		[TestCase(nameof(SwapPositionInListViewService.MoveObjectDown), 1)]
		[XpandTest]
		public void When_moving_object_swap_position_with_next_object(string direction, int otherObjectPosition){
			using (var applicatin = PositionInListViewModuleModule().Application){
				var modelPositionInListView = applicatin.Model.ToReactiveModule<IModelReactiveModulesPositionInListView>()
					.PositionInListView;
				var listViewItem = modelPositionInListView.ListViewItems.AddNode<IModelPositionInListViewListViewItem>();
				listViewItem.ListView = applicatin.Model.BOModel.GetClass(typeof(PIL)).DefaultListView;
				var objectSpace = applicatin.CreateObjectSpace();
				var pil1 = objectSpace.CreateObject<PIL>();
				pil1.Name = "a";
				var pil2 = objectSpace.CreateObject<PIL>();
				pil2.Name = "b";
				var pil3 = objectSpace.CreateObject<PIL>();
				pil3.Name = "c";
				objectSpace.CommitChanges();
				var viewWindow = applicatin.CreateViewWindow();
				viewWindow.SetView(applicatin.NewView(listViewItem.ListView));
				var action = viewWindow.Action(direction);
				var edgeObject = direction == nameof(SwapPositionInListViewService.MoveObjectUp) ? pil3 : pil1;

				action.DoExecute(space1 => new[]{space1.GetObject(edgeObject)});

				var space = applicatin.CreateObjectSpace();
				space.GetObject(edgeObject).Order.ShouldBe(2);
				space.GetObject(pil2).Order.ShouldBe(otherObjectPosition);
			}
		}

		[TestCase(nameof(SwapPositionInListViewService.MoveObjectUp))]
		[TestCase(nameof(SwapPositionInListViewService.MoveObjectDown))]
		[XpandTest]
		public void When_moving_edge_object_towards_edge_position_should_not_change(string direction){
			using (var applicatin = PositionInListViewModuleModule().Application){
				var modelPositionInListView = applicatin.Model.ToReactiveModule<IModelReactiveModulesPositionInListView>()
					.PositionInListView;
				var listViewItem = modelPositionInListView.ListViewItems.AddNode<IModelPositionInListViewListViewItem>();
				listViewItem.ListView = applicatin.Model.BOModel.GetClass(typeof(PIL)).DefaultListView;
				var objectSpace = applicatin.CreateObjectSpace();
				var pil1 = objectSpace.CreateObject<PIL>();
				pil1.Name = "a";
				var pil2 = objectSpace.CreateObject<PIL>();
				pil2.Name = "b";
				var pil3 = objectSpace.CreateObject<PIL>();
				pil3.Name = "c";
				objectSpace.CommitChanges();
				var viewWindow = applicatin.CreateViewWindow();
				viewWindow.SetView(applicatin.NewView(listViewItem.ListView));
				var action = viewWindow.Action(direction);
				var edgeObject = direction == nameof(SwapPositionInListViewService.MoveObjectUp) ? pil1 : pil3;

				action.DoExecute(space1 => new[]{space1.GetObject(edgeObject)});

				var space = applicatin.CreateObjectSpace();
				space.GetObject(edgeObject).Order
					.ShouldBe(direction == nameof(SwapPositionInListViewService.MoveObjectUp) ? 1 : 3);
				space.GetObject(pil2).Order.ShouldBe(2);
			}
		}
	}
}