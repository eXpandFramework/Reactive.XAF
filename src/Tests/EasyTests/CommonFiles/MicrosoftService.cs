using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using DevExpress.EasyTest.Framework;
using Xpand.TestsLib.Common.EasyTest;
using Xpand.TestsLib.Common.EasyTest.Commands;
using Xpand.TestsLib.Common.EasyTest.Commands.Automation;
using Xpand.TestsLib.Common.Win32;

namespace ALL.Tests{
#if !NETCOREAPP3_1
    public static class MicrosoftService{
        public static async Task TestMicrosoftService(this ICommandAdapter commandAdapter,Func<Task> whenConnected) 
            => await commandAdapter.TestCloudService(singInCaption 
                => commandAdapter.Authenticate(singInCaption,"DXMailPass","apostolisb@devexpress.com")
                    .Do(_ => commandAdapter.Execute(new SendKeysCommand(Win32Constants.VirtualKeys.Return),new WaitCommand(OfficeCloudService.WaitInterval)))
                    .SelectMany(_ => commandAdapter.PushToken("Azure").Concat(whenConnected().ToObservable())), "Microsoft",new CheckDetailViewCommand(("Mail","apostolisb@devexpress.com")));


    }
#endif
}