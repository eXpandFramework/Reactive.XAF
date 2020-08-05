using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.BytesExtensions;
using Xpand.Extensions.Office.Cloud;
using Xpand.XAF.Modules.Office.Cloud.Microsoft.BusinessObjects;
using Xpand.XAF.Modules.Office.Cloud.Tests;
using Xpand.XAF.Modules.Reactive;
using File = System.IO.File;
using Platform = Xpand.Extensions.XAF.XafApplicationExtensions.Platform;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft.Tests{
	public static class TestExtensions{
		public static void NewAuthentication(this IObjectSpaceProvider objectSpaceProvider,string serviceName,Platform platform=Platform.Win){
			objectSpaceProvider.NewAuthentication<MSAuthentication>((authentication, bytes) => authentication.Token=bytes.GetString(),serviceName,platform);
		}

		public static void ConfigureMicrosoft(this IModelApplication application){
			var json = JsonConvert.DeserializeObject<dynamic>(
				File.ReadAllText($"{AppDomain.CurrentDomain.ApplicationPath()}\\AzureAppCredentials.json"));
			var modelOAuth = application.ToReactiveModule<IModelReactiveModuleOffice>().Office.Microsoft().OAuth;
			modelOAuth.ClientId = json.MSClientId;
			modelOAuth.RedirectUri = json.RedirectUri;
			modelOAuth.ClientSecret = json.MSClientSecret;
		}

		public static IObservable<GraphServiceClient> AuthorizeTestMS(this XafApplication application,bool aquireToken=true){
			if (aquireToken){
				application.ObjectSpaceProvider.NewAuthentication("Microsoft");
			}
            
			return aquireToken ? application.AuthorizeMS() : application.AuthorizeMS((e, clientApp) => Observable.Throw<AuthenticationResult>(e));
		}
	}
}