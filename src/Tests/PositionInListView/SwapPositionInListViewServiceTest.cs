using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.CollectionSourceExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.PositionInListView.Tests.BOModel;

namespace Xpand.XAF.Modules.PositionInListView.Tests{
	public class SwapPositionInListViewServiceTest : PositionInListViewCommonTest{
		[Test]
		
		public void Move_object_actions_are_active_only_for_model_views() {
            using var application = PositionInListViewModuleModule().Application;
            
            var viewWindow = application.CreateViewWindow();

            viewWindow.SetView(application.NewView(ListViewItem.ListView));

            var moduleAction = viewWindow.Action<PositionInListViewModule>();
            moduleAction.MoveObjectUp().Active.ResultValue.ShouldBeTrue();
            moduleAction.MoveObjectDown().Active.ResultValue.ShouldBeTrue();
        }

		[TestCase(nameof(SwapPositionInListViewService.MoveObjectUp),nameof(SwapPositionInListViewService.MoveObjectDown))]
		[TestCase(nameof(SwapPositionInListViewService.MoveObjectDown),nameof(SwapPositionInListViewService.MoveObjectUp))]
		
		[SuppressMessage("ReSharper", "AccessToModifiedClosure")][Apartment(ApartmentState.STA)]
		public void Move_object_action_is_disabled_when_on_edge(string disabledAction,string enabledAction) {
            using var application = PositionInListViewModuleModule().Application;

            var objectSpace = application.CreateObjectSpace();

            var viewWindow = application.CreateViewWindow();
            PIL pil1 = null;
            PIL pil2 = null;
            var view = application.NewView(ListViewItem.ListView,
                space => new[]{space.GetObject(disabledAction == nameof(SwapPositionInListViewService.MoveObjectUp) ? pil1 : pil2)},objectSpace);
            viewWindow.SetView(view);
            pil1 = objectSpace.CreateObject<PIL>();
            pil1.Name = "a";
            pil2 = objectSpace.CreateObject<PIL>();
            pil2.Name = "b";
            objectSpace.CommitChanges();

            view.AsObjectView().OnSelectionChanged();


            viewWindow.Action(disabledAction).Enabled[SwapPositionInListViewService.EdgeContext].ShouldBeFalse();
            viewWindow.Action(enabledAction).Enabled[SwapPositionInListViewService.EdgeContext].ShouldBeTrue();
            view.AsListView().CollectionSource.Objects<PIL>().Count().ShouldBe(2);
        }

		[TestCase(nameof(SwapPositionInListViewService.MoveObjectUp), 3)]
		[TestCase(nameof(SwapPositionInListViewService.MoveObjectDown), 1)]
		
		public void When_moving_object_swap_position_with_next_object(string direction, int otherObjectPosition) {
			
            using var application = PositionInListViewModuleModule().Application;
            
            var objectSpace = application.CreateObjectSpace();
				
            var viewWindow = application.CreateViewWindow();
            var compositeView = application.NewView(ListViewItem.ListView,objectSpace).AsListView();
            viewWindow.SetView(compositeView);
            var pil1 = objectSpace.CreateObject<PIL>();
            pil1.Name = "a";
            var pil2 = objectSpace.CreateObject<PIL>();
            pil2.Name = "b";
            var pil3 = objectSpace.CreateObject<PIL>();
            pil3.Name = "c";
            compositeView.CollectionSource.Add(pil2);
            compositeView.CollectionSource.Add(pil1);
            compositeView.CollectionSource.Add(pil3);
            compositeView.ObjectSpace.CommitChanges();
            compositeView.CollectionSource.Objects().Count().ShouldBe(3);
            var action = viewWindow.Action(direction);
            var edgeObject = direction == nameof(SwapPositionInListViewService.MoveObjectUp) ? pil3 : pil1;

            action.DoExecute(space1 => new[]{space1.GetObject(edgeObject)});

            edgeObject.Order.ShouldBe(2);
            pil2.Order.ShouldBe(otherObjectPosition);
        }

		[TestCase(nameof(SwapPositionInListViewService.MoveObjectUp))]
		[TestCase(nameof(SwapPositionInListViewService.MoveObjectDown))]
		
		public void When_moving_edge_object_towards_edge_position_should_not_change(string direction) {
            using var application = PositionInListViewModuleModule().Application;
            
            var objectSpace = application.CreateObjectSpace();
            var pil1 = objectSpace.CreateObject<PIL>();
            pil1.Name = "a";
            var pil2 = objectSpace.CreateObject<PIL>();
            pil2.Name = "b";
            var pil3 = objectSpace.CreateObject<PIL>();
            pil3.Name = "c";
            objectSpace.CommitChanges();
            var viewWindow = application.CreateViewWindow();
            viewWindow.SetView(application.NewView(ListViewItem.ListView));
            var action = viewWindow.Action(direction);
            var edgeObject = direction == nameof(SwapPositionInListViewService.MoveObjectUp) ? pil1 : pil3;

            action.DoExecute(space1 => new[]{space1.GetObject(edgeObject)});

            var space = application.CreateObjectSpace();
            space.GetObject(edgeObject).Order
                .ShouldBe(direction == nameof(SwapPositionInListViewService.MoveObjectUp) ? 1 : 3);
            space.GetObject(pil2).Order.ShouldBe(2);
        }
	}
}