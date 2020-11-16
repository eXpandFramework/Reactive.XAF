using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Newtonsoft.Json;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.BytesExtensions;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.Extensions.XAF.Xpo.ValueConverters;
using Xpand.XAF.Modules.Office.Cloud.Google;
using Xpand.XAF.Modules.Office.Cloud.Google.BusinessObjects;
using Xpand.XAF.Modules.Reactive.Services.Actions;

// ReSharper disable once CheckNamespace
namespace TestApplication.GoogleService{
	public static class GoogleService{
        
        public static SimpleAction PersistGoogleToken(this (AgnosticModule module, Frame frame) tuple) 
            => tuple.frame.Action(nameof(PersistGoogleToken)).As<SimpleAction>();

		public static IObservable<Unit> PersistToken(this IObservable<SimpleActionExecuteEventArgs> source) 
			=> source.Do(e => {
					using var objectSpace = e.Action.Application.CreateObjectSpace();
					var authentication = objectSpace.GetObjectByKey<GoogleAuthentication>(SecuritySystem.CurrentUserId);
					if (authentication == null){
						authentication = objectSpace.CreateObject<GoogleAuthentication>();
						authentication.UserName = SecuritySystem.CurrentUserName;
						authentication.Oid = (Guid) SecuritySystem.CurrentUserId;
						var platform = AppDomain.CurrentDomain.IsHosted() ? Platform.Web : Platform.Win;
						string parentFolder = null;
						if (new[]{Platform.Blazor,Platform.Web }.Contains(platform)){
							parentFolder = "..\\";
							platform=Platform.Web;
						}

						string serviceName="Google";

						var bytes = File.ReadAllText($"{AppDomain.CurrentDomain.ApplicationPath()}\\..\\{parentFolder}{serviceName}AuthenticationData{platform}.json").Bytes();
						authentication.OAuthToken=(Dictionary<string, string>) new DictionaryValueConverter().ConvertFromStorageType(bytes.GetString());
						objectSpace.CommitChanges();
					}
				})
				.ToUnit();

		public static IObservable<Unit> ConnectGoogleService(this ApplicationModulesManager manager) 
            => manager.ConnectCloudService("Google",(AppDomain.CurrentDomain.IsHosted() ? Platform.Web : Platform.Win).ToString(),office => office.Google().OAuth)
                .WhenNotDefault()
				.Do(t => {
					var json = JsonConvert.DeserializeObject<dynamic>(t.creds!);

                    if (AppDomain.CurrentDomain.IsHosted()){
                        if (t.modelOAuth != null){
                            t.modelOAuth.ClientId = json.web.client_id;
                            t.modelOAuth.ClientSecret = json.web.client_secret;
                            t.modelOAuth.RedirectUri = json.web.redirect_uris[0];
                        }
                    }
                    else{
                        if (t.modelOAuth != null){
                            t.modelOAuth.ClientId = json.installed.client_id;
                            t.modelOAuth.ClientSecret = json.installed.client_secret;
                        }
                    }
					
				})
				.ToUnit()
                .Merge(manager.RegisterViewSimpleAction(nameof(PersistGoogleToken),
                    action => { }).ActivateInUserDetails().WhenExecute().PersistToken().ToUnit())
                .Merge(manager.ShowGoogleAccountInfo())
                .Merge(manager.PushTheToken<GoogleAuthentication>("Google",o => new DictionaryValueConverter().ConvertToStorageType(o.OAuthToken).ToString()))
                .ToUnit();


	}
}