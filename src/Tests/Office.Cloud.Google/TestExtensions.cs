using System;
using System.Collections.Generic;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Google.Apis.People.v1;
using Newtonsoft.Json;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.BytesExtensions;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.XAF.Xpo.ValueConverters;
using Xpand.XAF.Modules.Office.Cloud.Google.BusinessObjects;
using Xpand.XAF.Modules.Office.Cloud.Tests;
using Xpand.XAF.Modules.Reactive;
using File = System.IO.File;
using Platform = Xpand.Extensions.XAF.XafApplicationExtensions.Platform;

namespace Xpand.XAF.Modules.Office.Cloud.Google.Tests{
	public static class TestExtensions{
		public static void NewAuthentication(this IObjectSpaceProvider objectSpaceProvider,Platform platform=Platform.Win){
			objectSpaceProvider.NewAuthentication<GoogleAuthentication>((authentication, bytes) => {
				authentication.OAuthToken=(Dictionary<string, string>) new DictionaryValueConverter().ConvertFromStorageType(bytes.GetString());
			},"Google",platform);
		}

		public static void ConfigureGoogle(this IModelApplication application,Platform platform){
			var json = JsonConvert.DeserializeObject<dynamic>(
				File.ReadAllText($"{AppDomain.CurrentDomain.ApplicationPath()}\\Google{platform}AppCredentials.json"));
			var modelOAuth = application.ToReactiveModule<IModelReactiveModuleOffice>().Office.Google().OAuth;
			modelOAuth.Scopes = PeopleService.Scope.UserinfoProfile;
			if (platform == Platform.Win){
				modelOAuth.ClientId = json.installed.client_id;
				modelOAuth.ClientSecret = json.installed.client_secret;	
			}
			else{
				modelOAuth.ClientId = json.web.client_id;
				modelOAuth.ClientSecret = json.web.client_secret;
			}
			
        }
    }
}