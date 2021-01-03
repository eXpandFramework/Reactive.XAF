using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Newtonsoft.Json;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.XAF.Modules.Office.Cloud.Microsoft;
using Xpand.XAF.Modules.Office.Cloud.Microsoft.BusinessObjects;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace TestApplication.Module.Win.Office.Microsoft{
    public static class MicrosoftService{
        public static SimpleAction PushAzureToken(this (TestApplicationModule module, Frame frame) tuple) 
            => tuple.frame.Action(nameof(PushAzureToken)).As<SimpleAction>();

        public static IObservable<Unit> ConnectMicrosoftService(this ApplicationModulesManager manager)
            => manager.ConnectCloudService("Microsoft", null, office => office.Microsoft().OAuth)
                .WhenNotDefault()
                .Do(t => {
                    var json = JsonConvert.DeserializeObject<dynamic>(t.creds);
                    t.modelOAuth.ClientId = json.MSClientId;
                    t.modelOAuth.RedirectUri = json.RedirectUri;
                    t.modelOAuth.ClientSecret = json.MSClientSecret;
                    if (manager.Modules.OfType<IWebModule>().Any()){
                        t.modelOAuth.RedirectUri = "http://localhost:65477/login.aspx";
                    }
                })
                .ToUnit()
                .Merge(manager.PushTheToken<MSAuthentication>("Azure",o => o.Token))
                .Merge(manager.ShowMSAccountInfo());
    }
}