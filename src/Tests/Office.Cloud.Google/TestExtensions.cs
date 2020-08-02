using System;
using System.IO;
using System.Reactive.Linq;
using System.Text;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Newtonsoft.Json;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.BytesExtensions;
using Xpand.Extensions.Office.Cloud;
using Xpand.XAF.Modules.Office.Cloud.Google.BusinessObjects;
using Xpand.XAF.Modules.Office.Cloud.Tests;
using Xpand.XAF.Modules.Reactive;
using File = System.IO.File;
using Platform = Xpand.Extensions.XAF.XafApplicationExtensions.Platform;

namespace Xpand.XAF.Modules.Office.Cloud.Google.Tests{
	public static class TestExtensions{
		public static void NewAuthentication(this IObjectSpaceProvider objectSpaceProvider,Platform platform=Platform.Win){
			objectSpaceProvider.NewAuthentication<GoogleAuthentication>((authentication, bytes) => {
				// authentication.Token = bytes.GetString();
			},platform);
		}

		public static void ConfigureGoogle(this IModelApplication application){
			var json = JsonConvert.DeserializeObject<dynamic>(
				File.ReadAllText($"{AppDomain.CurrentDomain.ApplicationPath()}\\AzureAppCredentials.json"));
			var modelOAuth = application.ToReactiveModule<IModelReactiveModuleOffice>().Office.Google().OAuth;
			modelOAuth.ClientId = json.MSClientId;
			modelOAuth.RedirectUri = json.RedirectUri;
			modelOAuth.ClientSecret = json.MSClientSecret;
		}

		// public static IObservable<GraphServiceClient> AuthorizeTestMS(this XafApplication application,bool aquireToken=true){
		// 	if (aquireToken){
		// 		application.ObjectSpaceProvider.NewMicrosoftAuthentication();
		// 	}
  //           
		// 	return aquireToken ? application.AuthorizeMS() : application.AuthorizeMS((e, clientApp) => Observable.Throw<AuthenticationResult>(e));
		// }
	}
}