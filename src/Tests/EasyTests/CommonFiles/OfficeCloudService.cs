using System;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevExpress.EasyTest.Framework;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib.EasyTest;
using Xpand.TestsLib.EasyTest.Commands;
using Xpand.TestsLib.EasyTest.Commands.ActionCommands;
using Xpand.TestsLib.EasyTest.Commands.Automation;
using Xpand.TestsLib.Win32;

namespace ALL.Tests{
    public static class OfficeCloudService{
        public static IObservable<Unit> PushToken(this ICommandAdapter commandAdapter,string serviceName){
            // if (!commandAdapter.GetTestApplication().IsWeb()){
            //     commandAdapter.Execute(new ActionCommand($"Push{serviceName}Token"));
            // }
            return Observable.Empty<Unit>();
        } 
        private static async Task CheckOperation(this ICommandAdapter commandAdapter, MapAction mapAction){
            await commandAdapter.Execute(() => {
                commandAdapter.Execute(new ActionCommand(Actions.Refresh));
                commandAdapter.Execute(new CheckActionToolTip(("Save",mapAction.ToString())));
            });
        }

        public static async Task TestOfficeCloudService(this ICommandAdapter commandAdapter, string serviceName){
            commandAdapter.Execute(new NavigateCommand($"Cloud.{serviceName} Event"));
            var cloudCalendarOperation = "Cloud Calendar Operation";
            commandAdapter.Execute(new ActionCommand(cloudCalendarOperation, "New"),new WaitCommand(2500));
            await commandAdapter.Execute(() => {
                commandAdapter.Execute(new ActionCommand(Actions.Refresh));
                var checkListViewCommand = new CheckListViewCommand("Subject");
                checkListViewCommand.AddRows(new[]{$"{serviceName}-Cloud"});
                commandAdapter.Execute(checkListViewCommand);
            });
            commandAdapter.Execute(new ActionCommand(cloudCalendarOperation, "Update"),new WaitCommand(2500));
            await commandAdapter.Execute(() => {
                commandAdapter.Execute(new ActionCommand(Actions.Refresh));
                var checkListViewCommand = new CheckListViewCommand("Subject");
                checkListViewCommand.AddRows(new[]{$"{serviceName}-Cloud-Updated"});
                commandAdapter.Execute(checkListViewCommand);
            });
            commandAdapter.Execute(new ActionCommand(cloudCalendarOperation, "Delete"),new WaitCommand(2500));
            await commandAdapter.Execute(() => {
                commandAdapter.Execute(new ActionCommand(Actions.Refresh));
                commandAdapter.Execute(new CheckListViewCommand("", 0));
            });
        }

        public static async Task TestOfficeCloudService(this ICommandAdapter commandAdapter, string navigationItemCaption, string editorName){
            commandAdapter.Execute(new NavigateCommand(navigationItemCaption),new ActionCommand(Actions.New));
            commandAdapter.Execute(new FillObjectViewCommand((editorName, "New")),new WaitCommand(2000),new ActionCommand(Actions.Save));
            await commandAdapter.CheckOperation(MapAction.Insert);
            commandAdapter.Execute(new FillObjectViewCommand((editorName, "Update")));
            commandAdapter.Execute(new WaitCommand(2000));
            await commandAdapter.Execute(() => commandAdapter.Execute(new ActionCommand(Actions.Save)));
            await commandAdapter.CheckOperation(MapAction.Update);
            commandAdapter.Execute(new ActionDeleteCommand());
            commandAdapter.Execute(new NavigateCommand(navigationItemCaption));
            await commandAdapter.Execute(() => commandAdapter.Execute(new ActionCommand(Actions.Refresh), new CheckListViewCommand("", 0)));
        }

        public static async Task TestCloudService(this ICommandAdapter commandAdapter,
            Func<string, IObservable<Unit>> whenConnected, string serviceName,
            CheckDetailViewCommand checkAccountInfoCommand){
            var signOutCaption = $"Sign out {serviceName}";
            var signInCaption = $"Sign In {serviceName}";
            commandAdapter.Execute(new NavigateCommand("Default.My Details"),new ActionCommand(signOutCaption){ExpectException = true});
            
            await whenConnected(signInCaption);
            commandAdapter.CheckConnection(signOutCaption, signInCaption, checkAccountInfoCommand,serviceName);
            commandAdapter.Execute(new LogOffCommand(),new LoginCommand());
            commandAdapter.CheckConnection(signOutCaption, signInCaption, checkAccountInfoCommand,serviceName);
            commandAdapter.Execute(new ActionCloseCommand(),new ActionCloseCommand());
            commandAdapter.Disconnect(signOutCaption,signInCaption);
        }

        private static void CheckConnection(this ICommandAdapter commandAdapter, string signOutCaption,
            string signInCaption, Command checkAccountInfoCommand,string serviceName){
            commandAdapter.Execute(new NavigateCommand("Default.My Details"));
            commandAdapter.Execute(new ActionAvailableCommand(signInCaption){ExpectException = true});
            commandAdapter.Execute(new ActionAvailableCommand(signOutCaption));
            commandAdapter.Execute(new CheckActionToolTip((signOutCaption,"Sign out")));
            commandAdapter.Execute(new ActionCommand($"Show {serviceName} Account Info"));
            commandAdapter.Execute(new WaitCommand(WaitInterval));
            commandAdapter.Execute(checkAccountInfoCommand);
            commandAdapter.Execute(new ActionCloseCommand());
        }

        public static int WaitInterval => Debugger.IsAttached?2500:5000;

        private static void Disconnect(this ICommandAdapter commandAdapter, string signOutCaption, string signInCaption){
            commandAdapter.Execute(new NavigateCommand("Default.My Details"),new ActionCommand(signOutCaption),
                new ActionAvailableCommand(signOutCaption)
                    {ExpectException = true},
                new ActionAvailableCommand(signInCaption));
        }

        public static IObservable<Unit> Authenticate(this ICommandAdapter commandAdapter, string signInCaption,string passFileName,string email,Action afterSingInActionExecuted=null){
            if (commandAdapter.GetTestApplication().Platform()!=Platform.Win){
                Observable.Start(() => commandAdapter.Execute(new ActionCommand(signInCaption)))
                    .Timeout(TimeSpan.FromSeconds(Debugger.IsAttached?10:30)).OnErrorResumeNext(Observable.Empty<Unit>().FirstOrDefaultAsync()).Wait();
            }
            else{
                commandAdapter.Execute(new ActionCommand(signInCaption),new WaitCommand(WaitInterval*2));
            }
            commandAdapter.Execute(new WaitCommand(5000));
            var foregroundWindow = Win32Declares.WindowFocus.GetForegroundWindow();
            commandAdapter.Execute(new MoveWindowCommand(0,0,1024,768));
            afterSingInActionExecuted?.Invoke();
            commandAdapter.Execute(new SendTextCommand(email),new WaitCommand(1000));
            Win32Declares.WindowFocus.SetForegroundWindow(foregroundWindow);
            commandAdapter.Execute(new SendKeysCommand(Win32Constants.VirtualKeys.Return), new WaitCommand((int) (WaitInterval*1.5)));
            Win32Declares.WindowFocus.SetForegroundWindow(foregroundWindow);
            var passDirectory=$"{AppDomain.CurrentDomain.ApplicationPath()}\\..\\";
            if (!AppDomain.CurrentDomain.UseNetFramework()){
	            passDirectory += "..\\";
            }
            var dxMailPass = File.ReadAllText($"{passDirectory}{passFileName}.json").Trim();
            commandAdapter.Execute(new SendTextCommand(dxMailPass),new WaitCommand(1000),
                new SendKeysCommand(Win32Constants.VirtualKeys.Return), new WaitCommand(WaitInterval));
            Win32Declares.WindowFocus.SetForegroundWindow(foregroundWindow);
            return Unit.Default.ReturnObservable();
        }

    }
}