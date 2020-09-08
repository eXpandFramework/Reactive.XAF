using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ALL.Win.Tests;
using DevExpress.EasyTest.Framework;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Reactive.Transform;
using Xpand.TestsLib.EasyTest;
using Xpand.TestsLib.EasyTest.Commands;
using Xpand.TestsLib.EasyTest.Commands.ActionCommands;
using Xpand.TestsLib.EasyTest.Commands.Automation;
using Xpand.TestsLib.Win32;

namespace ALL.Tests{
    public static class OfficeCloudService{
        private static void CheckOperatrion(this ICommandAdapter commandAdapter, MapAction mapAction, string verifyEditor){
            commandAdapter.Execute(new WaitCommand(2000), new ActionCommand(Actions.Refresh));
            commandAdapter.Execute(new CheckDetailViewCommand((verifyEditor, mapAction.ToString())));
        }

        public static void TestOfficeCloudService(this ICommandAdapter commandAdapter, string navigationItemCaption,
            string editorName, string verifyEditor){
            commandAdapter.Execute(new NavigateCommand(navigationItemCaption),new ActionCommand(Actions.New));
            commandAdapter.Execute(new FillObjectViewCommand((editorName, "New")),
                new ActionCommand(Actions.Save));
            
            commandAdapter.CheckOperatrion(MapAction.Insert, verifyEditor);
                    
            commandAdapter.Execute(new FillObjectViewCommand((editorName, "Update")),new ActionCommand(Actions.Save));
            commandAdapter.CheckOperatrion(MapAction.Update, verifyEditor);

            commandAdapter.Execute(new ActionDeleteCommand());
            
            
            commandAdapter.Execute(new NavigateCommand(navigationItemCaption));
            commandAdapter.Execute(new ActionCommand(Actions.Refresh), new CheckListViewCommand("",0));
        }

        public static async Task TestCloudServices(this ICommandAdapter commandAdapter){
            await commandAdapter.TestGoogleService(() => Observable.Start(commandAdapter.TestGoogleTasksService).ToUnit());
            await commandAdapter.TestMicrosoftService(() => Observable.Start(() => {
                commandAdapter.TestMicrosoftCalendarService();
                commandAdapter.TestMicrosoftTodoService();
            }).ToUnit());
        }

        public static async Task TestCloudService(this ICommandAdapter commandAdapter,
            Func<string, IObservable<Unit>> whenConnected, string serviceName,
            CheckDetailViewCommand checkAccountInfoCOmmand){
            var signOutCaption = $"Sign out {serviceName}";
            var signInCaption = $"Sign In {serviceName}";
            commandAdapter.Execute(new NavigateCommand("Default.My Details"),new ActionCommand(signOutCaption){ExpectException = true});
            
            await whenConnected(signInCaption);
            commandAdapter.CheckConnection(signOutCaption, signInCaption, checkAccountInfoCOmmand,serviceName);
            commandAdapter.Execute(new LogOffCommand(),new LoginCommand());
            commandAdapter.CheckConnection(signOutCaption, signInCaption, checkAccountInfoCOmmand,serviceName);
            commandAdapter.Execute(new ActionCloseCommand(),new ActionCloseCommand());
            commandAdapter.Disconnect(signOutCaption,signInCaption);
        }

        private static void CheckConnection(this ICommandAdapter commandAdapter, string signOutCaption,
            string signInCaption, Command checkAccountInfoCOmmand,string serviceName){
            commandAdapter.Execute(new NavigateCommand("Default.My Details"),
                new ActionAvailableCommand(signInCaption){ExpectException = true},
                new ActionAvailableCommand(signOutCaption),
                new CheckActionToolTip((signOutCaption,"Sign out")),
                new ActionCommand($"Show {serviceName} Account Info"),new WaitCommand(WaitInterval),checkAccountInfoCOmmand,new ActionCloseCommand());
        }

        public static int WaitInterval => Debugger.IsAttached?2500:5000;

        private static void Disconnect(this ICommandAdapter commandAdapter, string signOutCaption, string signInCaption){
            commandAdapter.Execute(new NavigateCommand("Default.My Details"),new ActionCommand(signOutCaption),
                new ActionAvailableCommand(signOutCaption)
                    {ExpectException = true},
                new ActionAvailableCommand(signInCaption));
        }

        public static IObservable<Unit> Authenticate(this ICommandAdapter commandAdapter, string signInCaption,string passFileName,string email){
            if (commandAdapter.GetTestApplication().IsWeb()){
                Observable.Start(() => commandAdapter.Execute(new ActionCommand(signInCaption)))
                    .Timeout(TimeSpan.FromSeconds(Debugger.IsAttached?10:30)).OnErrorResumeNext(Observable.Empty<Unit>().FirstOrDefaultAsync()).Wait();
            }
            else{
                commandAdapter.Execute(new ActionCommand(signInCaption),new WaitCommand(WaitInterval*2));
            }
            commandAdapter.Execute(new WaitCommand(5000));
            var foregroundWindow = Win32Declares.WindowFocus.GetForegroundWindow();
            commandAdapter.Execute(new MoveWindowCommand(0,0,1024,768));
            // commandAdapter.Execute(new MouseCommand(new Point(341,663)),new WaitCommand(1000));
            // for (int i = 0; i < 70; i++){
            //     commandAdapter.Execute(new SendKeysCommand(Win32Constants.VirtualKeys.Up),new WaitCommand(150));    
            // }
            // for (int i = 0; i < 7; i++){
            //     commandAdapter.Execute(new SendKeysCommand(Win32Constants.VirtualKeys.Down),new WaitCommand(150));    
            // }
            // commandAdapter.Execute(new SendKeysCommand(Win32Constants.VirtualKeys.Return),new WaitCommand(150));    
            // commandAdapter.Execute(new WaitCommand(5000));
            commandAdapter.Execute(new SendTextCommand(email),new WaitCommand(1000));
            Win32Declares.WindowFocus.SetForegroundWindow(foregroundWindow);
            commandAdapter.Execute(new SendKeysCommand(Win32Constants.VirtualKeys.Return), new WaitCommand((int) (WaitInterval*1.5)));
            Win32Declares.WindowFocus.SetForegroundWindow(foregroundWindow);
            var dxMailPass = File.ReadAllText($"{AppDomain.CurrentDomain.ApplicationPath()}\\..\\{passFileName}.json").Trim();
            commandAdapter.Execute(new SendTextCommand(dxMailPass),new WaitCommand(1000),
                new SendKeysCommand(Win32Constants.VirtualKeys.Return), new WaitCommand(WaitInterval));
            Win32Declares.WindowFocus.SetForegroundWindow(foregroundWindow);
            return Unit.Default.ReturnObservable();
        }

    }
}