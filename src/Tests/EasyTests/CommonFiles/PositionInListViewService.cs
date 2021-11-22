using DevExpress.EasyTest.Framework;
using Xpand.TestsLib.EasyTest;
using Xpand.TestsLib.EasyTest.Commands;
using Xpand.TestsLib.EasyTest.Commands.ActionCommands;
using Xpand.XAF.Modules.PositionInListView;


namespace Win.Tests {
	public static class PositionInListViewService {
		public static void TestPositionInListView(this ICommandAdapter adapter) {
			adapter.Execute(new NavigateCommand("Default.Position In List View Object"));
			adapter.CreateObjects();

			adapter.CheckMoveUp();
			adapter.CheckMoveDown();
		}

		private static void CheckMoveDown(this ICommandAdapter adapter) {
			adapter.Execute(
				new ActionAvailableCommand(GetActionName(nameof(SwapPositionInListViewService.MoveObjectUp)))
					{ ExpectException = true });
			adapter.Execute(new ActionCommand(GetActionName(nameof(SwapPositionInListViewService.MoveObjectDown))));
			adapter.Execute(new ActionCommand(GetActionName(nameof(SwapPositionInListViewService.MoveObjectDown))));
			adapter.Execute(
				new ActionAvailableCommand(GetActionName(nameof(SwapPositionInListViewService.MoveObjectUp))));
			adapter.Execute(
				new ActionAvailableCommand(GetActionName(nameof(SwapPositionInListViewService.MoveObjectDown)))
					{ ExpectException = true });
			var checkListViewCommand = new CheckListViewCommand("Name", "IdVisible");
			checkListViewCommand.AddRows(true, new[] { "three", "3" });
			adapter.Execute(checkListViewCommand);
		}

		private static void CheckMoveUp(this ICommandAdapter adapter) {
			adapter.Execute(
				new ActionAvailableCommand(GetActionName(nameof(SwapPositionInListViewService.MoveObjectDown)))
					{ ExpectException = true });
			adapter.Execute(new ActionCommand(GetActionName(nameof(SwapPositionInListViewService.MoveObjectUp))));
			adapter.Execute(new ActionCommand(GetActionName(nameof(SwapPositionInListViewService.MoveObjectUp))));
			adapter.Execute(
				new ActionAvailableCommand(GetActionName(nameof(SwapPositionInListViewService.MoveObjectDown))));
			adapter.Execute(
				new ActionAvailableCommand(GetActionName(nameof(SwapPositionInListViewService.MoveObjectUp)))
					{ ExpectException = true });
			var checkListViewCommand = new CheckListViewCommand("Name", "IdVisible");
			checkListViewCommand.AddRows(true, new[] { "three", "1" });
			adapter.Execute(checkListViewCommand);
		}

		private static void CreateObjects(this ICommandAdapter adapter) {
			adapter.Execute(new ActionCommand(Actions.New));
			adapter.Execute(new FillObjectViewCommand(("Name", "one")));
			adapter.Execute(new ActionCommand(Actions.SaveAndClose));
			adapter.Execute(new ActionCommand(Actions.New));
			adapter.Execute(new FillObjectViewCommand(("Name", "two")));
			adapter.Execute(new ActionCommand(Actions.SaveAndClose));
			adapter.Execute(new ActionCommand(Actions.New));
			adapter.Execute(new FillObjectViewCommand(("Name", "three")));
			adapter.Execute(new ActionCommand(Actions.SaveAndClose));

			adapter.Execute(new SelectObjectsCommand("Name", new[] { "three" }));
		}

		private static string GetActionName(string action) => action.Replace("Object", "");
	}
}