using System;
using System.Drawing;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.EasyTest.Framework;
using NUnit.Framework;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.TestsLib.EasyTest;
using Xpand.TestsLib.EasyTest.Commands;
using Xpand.TestsLib.EasyTest.Commands.Automation;
using Xpand.TestsLib.Win32;

namespace ALL.Tests{
	public static class GoogleService{
        public static async Task TestGoogleService(this ICommandAdapter commandAdapter,Func<IObservable<Unit>> whenConnected) 
            => await commandAdapter.TestCloudService(singInCaption 
                => commandAdapter.Authenticate(singInCaption,"TestAppPass","xpand.testaplication@gmail.com")
                    .Concat(Unit.Default.ReturnObservable().SelectMany(_ => {
                        commandAdapter.Execute(new WaitCommand(3000));
                        TestContext.Out.WriteLine($"aaaaaaaaaaaaaaaaaaa{AppDomain.CurrentDomain.ApplicationPath()}\\..\\WinAuth.exe");
                        commandAdapter.Execute(new TwoFactorCommand($"{AppDomain.CurrentDomain.ApplicationPath()}\\..\\WinAuth.exe",$"{AppDomain.CurrentDomain.ApplicationPath()}\\..\\Xpand.testapplication.xml"));
                        commandAdapter.Execute(new SendKeysCommand(Win32Constants.VirtualKeys.Tab));
                        commandAdapter.Execute(new SendKeysCommand(Win32Constants.VirtualKeys.Tab));
                        commandAdapter.Execute(new SendKeysCommand(Win32Constants.VirtualKeys.Return));
                        commandAdapter.Execute(new WaitCommand(6000));
                        commandAdapter.Execute(new MouseCommand(new Point(100,100)));
                        try{
                            commandAdapter.Execute(new FindItemCommand("Verify",false),new WaitCommand(1500));
                            var text = Clipboard.GetText();
                            commandAdapter.Execute(new SendKeysCommand(Win32Constants.VirtualKeys.Tab));
                            commandAdapter.Execute(new SendKeysCommand(Win32Constants.VirtualKeys.Return));
                            commandAdapter.Execute(new WaitCommand(3000));
                        }
                        catch{
                            // ignored
                        }


                        commandAdapter.Execute(new MouseCommand(new Point(242,428)));
                        commandAdapter.Execute(new WaitCommand(3000));
                        commandAdapter.Execute(new MouseCommand(new Point(298,570)));
                        commandAdapter.Execute(new WaitCommand(6000));
                        commandAdapter.Execute(new MouseCommand(new Point(619, 497)),new WaitCommand(2000));
                        
                        
                        commandAdapter.Execute(new SendKeysCommand(Win32Constants.VirtualKeys.PageDown),new WaitCommand(2000));
                        commandAdapter.Execute(new SendKeysCommand(Win32Constants.VirtualKeys.PageDown),new WaitCommand(2000));
                        commandAdapter.Execute(new MouseCommand(new Point(652,606)),new WaitCommand(2000));
                        return whenConnected();
                    })), "Google",new CheckDetailViewCommand(("Value","xpand.testaplication@gmail.com")));
    }
}