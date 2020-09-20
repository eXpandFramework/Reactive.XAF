using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using DevExpress.EasyTest.Framework;
using Xpand.TestsLib.EasyTest;
using Xpand.TestsLib.EasyTest.Commands;
using Xpand.TestsLib.EasyTest.Commands.Automation;
using Xpand.TestsLib.Win32;

namespace ALL.Tests{
	public static class MicrosoftService{
        public static async Task TestMicrosoftService(this ICommandAdapter commandAdapter,Func<Task> whenConnected) 
            => await commandAdapter.TestCloudService(singInCaption 
                => commandAdapter.Authenticate(singInCaption,"DXMailPass","apostolisb@devexpress.com")
                    .Do(_ => commandAdapter.Execute(new SendKeysCommand(Win32Constants.VirtualKeys.Return),new WaitCommand(OfficeCloudService.WaitInterval)))
                    .SelectMany(_ => whenConnected().ToObservable()), "Microsoft",new CheckDetailViewCommand(("Mail","apostolisb@devexpress.com")));


    }
}