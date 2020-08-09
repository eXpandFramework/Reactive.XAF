using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ALL.Win.Tests;
using DevExpress.EasyTest.Framework;
using Xpand.TestsLib.EasyTest.Commands;

namespace ALL.Tests{
	public static class MicrosoftService{
        public static async Task TestMicrosoftService(this ICommandAdapter commandAdapter,Func<IObservable<Unit>> whenConnected) 
            => await commandAdapter.TestCloudService(singInCaption 
                => commandAdapter.Authenticate(singInCaption,"DXMailPass","apostolisb@devexpress.com")
                    .Concat(whenConnected()), "Microsoft",new CheckDetailViewCommand(("Mail","apostolisb@devexpress.com")));


    }
}