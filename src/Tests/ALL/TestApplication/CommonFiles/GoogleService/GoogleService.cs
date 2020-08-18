using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using Google.Apis.Tasks.v1;
using Newtonsoft.Json;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Office.Cloud.Google;
using Xpand.XAF.Modules.Reactive.Services;

// ReSharper disable once CheckNamespace
namespace TestApplication.GoogleService{
	public static class GoogleService{
		public static IObservable<Unit> ConnectGoogleService(this ApplicationModulesManager manager) =>
        manager.ConnectCloudService("Google",(AppDomain.CurrentDomain.IsHosted() ? Platform.Web : Platform.Win).ToString(),office => office.Google().OAuth)
                .WhenNotDefault()
				.Do(t => {
					var json = JsonConvert.DeserializeObject<dynamic>(t.creds);

                    if (AppDomain.CurrentDomain.IsHosted()){
                        t.modelOAuth.ClientId = json.web.client_id;
                        t.modelOAuth.ClientSecret = json.web.client_secret;
                    }
                    else{
                        t.modelOAuth.ClientId = json.installed.client_id;
                        t.modelOAuth.ClientSecret = json.installed.client_secret;
                    }
				})
				.ToUnit()
                .Merge(manager.ShowGoogleAccountInfo())
                // .Merge(manager.WhenApplication(application => SelectMany(application))
	                
	                .ToUnit();

		private static IObservable<char> SelectMany(XafApplication application){
			return application.WhenLoggedOn().SelectMany(t =>
					application.AuthorizeGoogle().Select(credential => credential).NewService<TasksService>())
				.SelectMany(service => service.Tasklists.List().ExecuteAsync())
				.SelectMany(lists => lists.Items.First().Id);
		}
	}
}