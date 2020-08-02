using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using Newtonsoft.Json;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.Office.Cloud.Microsoft;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
// ReSharper disable once CheckNamespace
namespace TestApplication.MicrosoftService{
	public static class MicrosoftService{
		public static IObservable<Unit> ConnectMicrosoftService(this ApplicationModulesManager manager) =>
			manager.WhenGeneratingModelNodes(application => application.Views)
				.Do(views => {
					var isWeb = manager.Modules.OfType<AgnosticModule>().First().Name.StartsWith("Web");
					string parentFolder = null;
					if (isWeb){
						parentFolder = "..\\";
					}
					var modelOAuth = views.Application.ToReactiveModule<IModelReactiveModuleOffice>().Office.Microsoft().OAuth;
					using (var manifestResourceStream = File.OpenRead($"{AppDomain.CurrentDomain.ApplicationPath()}\\..\\{parentFolder}AzureAppCredentials.json")){
						var json = JsonConvert.DeserializeObject<dynamic>(new StreamReader(manifestResourceStream!).ReadToEnd());
						modelOAuth.ClientId = "984844117367-e4j0tjtagncoi74b4eujupc99fc10svu.apps.googleusercontent.com";
						modelOAuth.RedirectUri = "";
						
						if (isWeb){
							modelOAuth.RedirectUri = "http://localhost:65477/login.aspx";
						}
						
						modelOAuth.ClientSecret = "-vyhiFsLx8qXiX1lscumx7g5";
						modelOAuth.Prompt=OAuthPrompt.Login;
					}
				})
				.ToUnit()
				.Merge(manager.ShowMSAccountInfo());
	}
}