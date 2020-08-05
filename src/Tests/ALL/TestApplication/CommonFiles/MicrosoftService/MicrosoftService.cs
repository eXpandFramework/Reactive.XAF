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
						modelOAuth.ClientId = json.MSClient;
						modelOAuth.RedirectUri = json.RedirectUri;
						
						if (isWeb){
							modelOAuth.RedirectUri = "http://localhost:65477/login.aspx";
						}
						
						modelOAuth.ClientSecret = json.MSClientSecret;
						modelOAuth.Prompt=OAuthPrompt.Login;
					}
				})
				.ToUnit()
				.Merge(manager.ShowMSAccountInfo());
	}
}