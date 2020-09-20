using System;
using System.Drawing;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using DevExpress.EasyTest.Framework;
using Xpand.Extensions.Reactive.Transform;
using Xpand.TestsLib.EasyTest;
using Xpand.TestsLib.EasyTest.Commands;
using Xpand.TestsLib.EasyTest.Commands.Automation;
using Xpand.TestsLib.Win32;

namespace ALL.Tests{
	public static class GoogleService{
        public static async Task TestGoogleService(this ICommandAdapter commandAdapter,Func<Task> whenConnected) 
            => await commandAdapter.TestCloudService(singInCaption 
                => commandAdapter.Authenticate(singInCaption,"TestAppPass","xpanddevops@gmail.com")
                    .Concat(Unit.Default.ReturnObservable().SelectMany(_ => {
                        if (!commandAdapter.GetTestApplication().IsWeb()){
                            commandAdapter.Execute(new MoveWindowCommand(0,0,1024,768));
                            commandAdapter.Execute(new FindItemCommand("Advance"),new WaitCommand(3000));
                            commandAdapter.Execute(new FindItemCommand("unsafe"),new WaitCommand(7000));
                            commandAdapter.Execute(new MouseCommand(new Point(619, 497)),new WaitCommand(2000));
                            commandAdapter.Execute(new SendKeysCommand(Win32Constants.VirtualKeys.PageDown),new WaitCommand(2000));
                            commandAdapter.Execute(new SendKeysCommand(Win32Constants.VirtualKeys.PageDown),new WaitCommand(2000));
                            commandAdapter.Execute(new MouseCommand(new Point(652,606)),new WaitCommand(2000));
                        }
                        return whenConnected().ToObservable();
                    })), "Google",new CheckDetailViewCommand(("Value","xpanddevops@gmail.com")));
    }
}