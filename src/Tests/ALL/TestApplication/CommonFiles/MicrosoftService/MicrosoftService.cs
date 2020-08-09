using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using Newtonsoft.Json;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Office.Cloud.Microsoft;

// ReSharper disable once CheckNamespace
namespace TestApplication.MicrosoftService{
	public static class MicrosoftService{
		public static IObservable<Unit> ConnectMicrosoftService(this ApplicationModulesManager manager)
        => manager.ConnectCloudService("Microsoft", null, office => office.Microsoft().OAuth)
            .WhenNotDefault()
            .Do(t => {
                var json = JsonConvert.DeserializeObject<dynamic>(t.creds);
                t.modelOAuth.ClientId = json.MSClientId;
                t.modelOAuth.RedirectUri = json.RedirectUri;
                t.modelOAuth.ClientSecret = json.MSClientSecret;
                var isWeb = manager.Modules.OfType<AgnosticModule>().First().Name.StartsWith("Web");
                if (isWeb){
                    t.modelOAuth.RedirectUri = "http://localhost:65477/login.aspx";
                }
            })
            .ToUnit()
            .Merge(manager.ShowMSAccountInfo());
	}
}