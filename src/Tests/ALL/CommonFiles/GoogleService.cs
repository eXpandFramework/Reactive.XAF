using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ALL.Win.Tests;
using DevExpress.EasyTest.Framework;
using Xpand.TestsLib.EasyTest.Commands;

namespace ALL.Tests{
	public static class GoogleService{
        public static async Task TestGoogleService(this ICommandAdapter commandAdapter,Func<IObservable<Unit>> whenConnected) 
            => await commandAdapter.TestCloudService(singInCaption 
                => commandAdapter.Authenticate(singInCaption,"TestAppPass","xpand.testaplication@gmail.com")
                    .Concat(whenConnected()), "Google",new CheckDetailViewCommand(("Value","xpand.testaplication@gmail.com")));
    }
}