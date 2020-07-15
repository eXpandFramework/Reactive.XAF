using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.EasyTest.Framework;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.TestsLib.EasyTest;
using Xpand.TestsLib.EasyTest.Commands;
using Xpand.TestsLib.EasyTest.Commands.ActionCommands;
using ActionAvailableCommand = Xpand.TestsLib.EasyTest.Commands.ActionCommands.ActionAvailableCommand;
using ActionCommand = Xpand.TestsLib.EasyTest.Commands.ActionCommands.ActionCommand;

namespace ALL.Tests{
	public static class MicrosoftService{
		public static async Task TestMicrosoftService(this ICommandAdapter commandAdapter,Func<IObservable<Unit>> whenConnected){
			
			var commands=new Command[]{
				new NavigateCommand("Default.My Details"),new ActionCommand(Xpand.XAF.Modules.Office.Cloud.Microsoft.MicrosoftService.SignOutCaption){ExpectException = true}
			};
			commandAdapter.Execute(commands);
			await commandAdapter.Authenticate();
			commandAdapter.CheckConnection();
			commandAdapter.Execute(new LogOffCommand(),new LoginCommand());
			commandAdapter.CheckConnection();
            await whenConnected();
			commandAdapter.Disconnect();
		}

		private static void Disconnect(this ICommandAdapter commandAdapter){
			commandAdapter.Execute(new NavigateCommand("Default.My Details"),new ActionCommand(Xpand.XAF.Modules.Office.Cloud.Microsoft.MicrosoftService.SignOutCaption),
				new ActionAvailableCommand(Xpand.XAF.Modules.Office.Cloud.Microsoft.MicrosoftService.SignOutCaption)
					{ExpectException = true},
				new ActionAvailableCommand(Xpand.XAF.Modules.Office.Cloud.Microsoft.MicrosoftService.SignInCaption));
		}

		private static void CheckConnection(this ICommandAdapter commandAdapter){
			commandAdapter.Execute(new NavigateCommand("Default.My Details"),
				new ActionAvailableCommand(Xpand.XAF.Modules.Office.Cloud.Microsoft.MicrosoftService.SignInCaption){ExpectException = true},
				new ActionAvailableCommand(Xpand.XAF.Modules.Office.Cloud.Microsoft.MicrosoftService.SignOutCaption),
				new CheckActionToolTip((Xpand.XAF.Modules.Office.Cloud.Microsoft.MicrosoftService.SignOutCaption,"Sign out apostolisb@devexpress.com")),
				new ActionCommand("Show MSAccount Info"),new WaitCommand(5000),new CheckDetailViewCommand(("Mail","apostolisb@devexpress.com")),new ActionOKCommand());
		}

		private static async Task Authenticate(this ICommandAdapter commandAdapter){
			if (commandAdapter.GetTestApplication().IsWeb()){
				
				await Observable.Start(() => commandAdapter.Execute(new ActionCommand(Xpand.XAF.Modules.Office.Cloud.Microsoft.MicrosoftService.SignInCaption)))
					.Timeout(TimeSpan.FromSeconds(30)).OnErrorResumeNext(Observable.Empty<Unit>().FirstOrDefaultAsync());
			}
			else{
				commandAdapter.Execute(new ActionCommand(Xpand.XAF.Modules.Office.Cloud.Microsoft.MicrosoftService.SignInCaption),new WaitCommand(10000));
				
			}
			commandAdapter.Execute(new PasteClipBoardCommand("apostolisb@devexpress.com"), new SendKeysCommand("{Enter}"), new WaitCommand(7000));

			var dxMailPass = File.ReadAllText($"{AppDomain.CurrentDomain.ApplicationPath()}\\..\\DXMailPass.json").Trim();
			commandAdapter.Execute(new PasteClipBoardCommand(dxMailPass),new WaitCommand(1000),
				new SendKeysCommand("{Enter}"), new WaitCommand(5000));
			commandAdapter.Execute(new SendKeysCommand("{Enter}"),new WaitCommand(5000));
		}
	}
}