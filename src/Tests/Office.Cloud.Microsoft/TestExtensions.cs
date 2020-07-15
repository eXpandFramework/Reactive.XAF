using System;
using System.IO;
using System.Reactive.Linq;
using System.Text;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.BytesExtensions;
using Xpand.Extensions.Office.Cloud;
using Xpand.XAF.Modules.Office.Cloud.Microsoft.BusinessObjects;
using Xpand.XAF.Modules.Reactive;
using File = System.IO.File;
using Platform = Xpand.Extensions.XAF.XafApplicationExtensions.Platform;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft.Tests{
	public static class TestExtensions{
		public static void NewMicrosoftAuthentication(this IObjectSpaceProvider objectSpaceProvider,Platform platform=Platform.Win){
			using (var manifestResourceStream = File.OpenRead($"{AppDomain.CurrentDomain.ApplicationPath()}\\AuthenticationData{platform}.json")){
				var token = Encoding.UTF8.GetBytes(new StreamReader(manifestResourceStream).ReadToEnd());
				using (var objectSpace = objectSpaceProvider.CreateObjectSpace()){
					var authenticationOid = (Guid)objectSpace.GetKeyValue(SecuritySystem.CurrentUser);
					if (objectSpace.GetObjectByKey<MSAuthentication>(authenticationOid)==null){
						var authentication = objectSpace.CreateObject<MSAuthentication>();
                    
						authentication.Oid=authenticationOid;
						authentication.Token=token.GetString();
						objectSpace.CommitChanges();
					}
				}
			}
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
				application.ObjectSpaceProvider.NewMicrosoftAuthentication();
			}
            
			return aquireToken ? application.AuthorizeMS() : application.AuthorizeMS((e, clientApp) => Observable.Throw<AuthenticationResult>(e));
		}
	}
}