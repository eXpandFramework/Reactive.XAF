using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.BytesExtensions;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.XAF.Xpo.ValueConverters;
using Xpand.TestsLib;
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

		public static void SetupGoogleSecurity(this XafApplication application, Platform platform) 
			=> application.SetupSecurity(platform==Platform.Win? Guid.Parse("4acb47a5-1d32-4720-be2d-f9ffd4a65c3e"):Guid.Parse("b43f6e81-0134-4dfb-8bd8-8178bc8534e6"));

		public static void ConfigureGoogle(this IModelApplication application,Platform platform){
			var json = JsonConvert.DeserializeObject<dynamic>(
				File.ReadAllText($"{AppDomain.CurrentDomain.ApplicationPath()}\\Google{platform}AppCredentials.json"));
			var modelOAuth = application.ToReactiveModule<IModelReactiveModuleOffice>().Office.Google().OAuth;
			modelOAuth.AddScopes("https://www.googleapis.com/auth/tasks","https://www.googleapis.com/auth/calendar.events");
            
            if (platform == Platform.Win){
				modelOAuth.ClientId = json.installed.client_id;
				modelOAuth.ClientSecret = json.installed.client_secret;	
			}
			else{
				modelOAuth.ClientId = json.web.client_id;
				modelOAuth.ClientSecret = json.web.client_secret;
                modelOAuth.RedirectUri = json.web.redirect_uris[0];
			}
			
        }

        public static IObservable<UserCredential> AuthorizeTestGoogle(this XafApplication application,bool aquireToken=true){
            if (aquireToken){
                application.ObjectSpaceProvider.NewAuthentication();
            }
            return aquireToken ? application.AuthorizeGoogle() : application.GoogleNeedsAuthentication()
                    .Select(b => b ? Observable.Throw<UserCredential>(new Exception(nameof(AuthorizeTestGoogle)))
                            : application.AuthorizeGoogle()).Merge();
        }

    }
}