using DevExpress.EasyTest.Framework;
using DevExpress.Persistent.BaseImpl;
using Xpand.TestsLib.EasyTest;
using Xpand.TestsLib.EasyTest.Commands;
using Xpand.TestsLib.EasyTest.Commands.ActionCommands;
using ActionCommand = Xpand.TestsLib.EasyTest.Commands.ActionCommands.ActionCommand;
using TaskStatus = DevExpress.Persistent.Base.General.TaskStatus;

namespace Web.Tests {
	public static class BulkObjectUpdateService {
		public static void TestBulkObjectUpdate(this ICommandAdapter adapter) {
			adapter.Execute(new NavigateCommand("Default.TestTask"));
			adapter.Execute(new ActionCommand(Actions.New));
			adapter.Execute(new FillObjectViewCommand<Task>((task => task.Subject, "1")));
			adapter.Execute(new ActionCommand(Actions.SaveAndClose));
			adapter.Execute(new ActionCommand(Actions.New));
			adapter.Execute(new FillObjectViewCommand<Task>((task => task.Subject, "2")));
			adapter.Execute(new ActionCommand(Actions.SaveAndClose));
			adapter.Execute(new SelectObjectsCommand(nameof(Task.Subject), new[] { "1", "2" }));
			adapter.Execute(
				new ActionCommand(nameof(Xpand.XAF.Modules.BulkObjectUpdate.BulkObjectUpdateService.BulkUpdate)));
			adapter.Execute(new FillObjectViewCommand<Task>((task => task.Status, nameof(TaskStatus.Deferred))));
			adapter.Execute(new ActionCommand(Actions.OK));
			adapter.Execute(new NavigateCommand("Default.User"));
			adapter.Execute(new NavigateCommand("Default.TestTask"));
			var checkListViewCommand = new CheckListViewCommand(new[] { nameof(Task.Subject), nameof(Task.Status) });
			checkListViewCommand.AddRows(new[] { "1", nameof(TaskStatus.Deferred) });
			checkListViewCommand.AddRows(true, new[] { "2", nameof(TaskStatus.Deferred) });
			adapter.Execute(checkListViewCommand);
			adapter.Execute(new SelectObjectsCommand(nameof(Task.Subject), new[] { "1", "2" }));
			adapter.Execute(new ActionDeleteObjectsCommand());
		}
	}
}