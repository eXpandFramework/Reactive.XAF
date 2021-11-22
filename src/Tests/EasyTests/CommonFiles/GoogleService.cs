using System;
using System.Reactive;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using DevExpress.EasyTest.Framework;
using Xpand.TestsLib.Common.BO;
using Xpand.TestsLib.EasyTest;
using Xpand.TestsLib.EasyTest.Commands;
using Xpand.TestsLib.EasyTest.Commands.ActionCommands;

namespace ALL.Tests {
	public static class GoogleService {
		public static async Task TestGoogleService(this ICommandAdapter commandAdapter, Func<Task> whenConnected)
			=> await commandAdapter.TestCloudService(_ => commandAdapter.AuthenticateGoogle(whenConnected), "Google",
				new CheckDetailViewCommand(("Value", "xpanddevops@gmail.com")));

		private static IObservable<Unit> AuthenticateGoogle(this ICommandAdapter commandAdapter,
			Func<Task> whenConnected) {
			commandAdapter.Execute(new ActionCommand("PersistGoogleToken"));
			commandAdapter.Execute(new NavigateCommand($"Default.{nameof(Order)}"));
			commandAdapter.Execute(new NavigateCommand("My Details"));
			commandAdapter.Execute(new LogOffCommand());
			commandAdapter.Execute(new LoginCommand());
			return whenConnected().ToObservable();
		}
	}
}