using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using JetBrains.Annotations;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Notifications;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.XAF.AppDomainExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ViewExtenions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Office.Cloud.Microsoft;
using Xpand.XAF.Modules.Office.Cloud.Microsoft.BusinessObjects;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Reactive.Services.Security;
using Platform = Xpand.Extensions.XAF.XafApplicationExtensions.Platform;
using Prompt = Microsoft.Identity.Client.Prompt;

[assembly: OwinStartup(typeof(MicrosoftService))]
namespace Xpand.XAF.Modules.Office.Cloud.Microsoft{
	public static class MicrosoftService{
		// public const string SignInCaption = "Sign in with Microsoft";
		// public const string SignOutCaption = "Sign out Microsoft";
		static readonly Subject<GenericEventArgs<IObservable<AuthenticationResult>>> CustomAquireTokenInteractivelySubject=new Subject<GenericEventArgs<IObservable<AuthenticationResult>>>();
		private static readonly Uri AuthorityUri=new Uri(string.Format(CultureInfo.InvariantCulture, "https://login.microsoftonline.com/{0}{1}", "common", "/v2.0"));
        
        private static readonly string AuthenticationType=OpenIdConnectAuthenticationDefaults.AuthenticationType;

        public static IObservable<bool> MicrosoftNeedsAuthentication(this XafApplication application) 
            => application.NeedsAuthentication<MSAuthentication>(() 
                => application.AuthorizeMS((exception, client) => Observable.Throw<AuthenticationResult>(exception)).ToUnit());

        public static SimpleAction ConnectMicrosoft(this (MicrosoftModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(ConnectMicrosoft)).As<SimpleAction>();

        public static SimpleAction DisconnectMicrosoft(this (MicrosoftModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(DisconnectMicrosoft)).As<SimpleAction>();

        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) 
            => manager.Connect("Microsoft", typeof(MSAuthentication), application 
                => application.MicrosoftNeedsAuthentication(), application 
                => application.AuthorizeMS((exception, app) => app.AquireTokenInteractively(application)).ToUnit());

        // private static IObservable<Unit> ExecuteActions(this IObservable<SimpleAction> registerActions) =>
	       //  registerActions.ActivateWhenUserDetails()
		      //   .SelectMany(action => action.Activate()
		      //   .Merge(action.Execution())
	       //  ).ToUnit();

      //   private static IObservable<SimpleAction> Execution(this SimpleAction action) => action
	     //    .WhenExecute(e => {
		    //     var execute = e.Action.Id == nameof(DisconnectMicrosoft)
			   //      ? e.Action.Application.NewObjectSpace(space => {
				  //       var objectSpace = e.Action.View().ObjectSpace;
				  //       objectSpace.Delete(objectSpace.GetObjectByKey<MSAuthentication>(e.Action.Application.CurrentUserId()));
				  //       objectSpace.CommitChanges();
						// e.Action.Data.Clear();
				  //       return e.Action.AsSimpleAction().ReturnObservable();
			   //      })
			   //      : e.Action.Application.AuthorizeMS((exception, app) => app.AquireTokenInteractively(action.Application)).To(e.Action.AsSimpleAction());
		    //     return execute.ActivateWhenAuthenticationNeeded();
	     //    })
	     //    .TraceMicrosoftModule();


        // private static IObservable<SimpleAction> Activate(this SimpleAction action){
	       //  return action.Application.MicrosoftNeedsAuthentication()
		      //   .Do(b => {
			     //    action.Active(nameof(MicrosoftNeedsAuthentication), action.Id == nameof(ConnectMicrosoft) ? b : !b);
			     //    if (!b && action.Id == nameof(DisconnectMicrosoft)){
				    //     action.UpdateDisconnectActionToolTip();
			     //    }
		      //   })
		      //   .To(action)
		      //   .TraceMicrosoftModule(a => $"{a.Id}, Active:{a.Active}");
        // }

      //   private static IObservable<Unit> ConfigureStyle(this IObservable<SimpleAction> source) => source
	     //    .WhenCustomizeControl()
	     //    .Select(_ => {
		    //     var application = _.sender.Application;
		    //     if (application.GetPlatform()==Platform.Web){
			   //      if (_.sender.Id == nameof(ConnectMicrosoft)){
						// _.sender.Model.SetValue("IsPostBackRequired",true);
			   //      }
			   //      var menuItem = _.e.Control.GetPropertyValue("MenuItem");
			   //      var itemStyle = menuItem.GetPropertyValue("ItemStyle");
			   //      itemStyle.GetPropertyValue("Paddings").SetPropertyValue("Padding", new System.Web.UI.WebControls.Unit(2));
			   //      itemStyle.SetPropertyValue("ImageSpacing", new System.Web.UI.WebControls.Unit(7));
			   //      itemStyle.GetPropertyValue("Font").SetPropertyValue("Name", "Roboto Medium");
			   //      itemStyle.GetPropertyValue("SelectedStyle").SetPropertyValue("BackColor", Color.White);
			   //      itemStyle.SetPropertyValue("ForeColor", ColorTranslator.FromHtml("#757575"));
			   //      itemStyle.GetPropertyValue("HoverStyle").SetPropertyValue("BackColor", Color.White);
			   //      menuItem.CallMethod("ForceMenuRendering");
		    //     }
		    //     return _.sender;
	     //    })
	     //    .ToUnit();
        
        // private static IObservable<SimpleAction> ActivateWhenUserDetails(this IObservable<SimpleAction> registerActions) =>
	       //  registerActions.ActivateInUserDetails(true)
		      //   .Do(action => action.Active(nameof(MicrosoftNeedsAuthentication),false) )
		      //   .Publish().RefCount();


       //  private static IObservable<SimpleAction> ActivateWhenAuthenticationNeeded(this IObservable<SimpleAction> source) =>
	      //   source.SelectMany(action => action.Application.MicrosoftNeedsAuthentication()
			    //     .Do(b => {
				   //      var actions = action.Controller.Frame.Action<MicrosoftModule>();
				   //      if (action.Id == nameof(ConnectMicrosoft)){
					  //       action.Active(nameof(MicrosoftNeedsAuthentication), b);
							// actions.DisconnectMicrosoft().Active(nameof(MicrosoftNeedsAuthentication),!b);
				   //      }
				   //      else{
					  //       action.Active(nameof(MicrosoftNeedsAuthentication), !b);
					  //       actions.ConnectMicrosoft().Active(nameof(MicrosoftNeedsAuthentication),b);
				   //      }
				   //      action.UpdateDisconnectActionToolTip();
			    //     }).To(action))
		     //    .WhenActive()
		     //    .TraceMicrosoftModule();

        // private static void UpdateDisconnectActionToolTip(this SimpleAction action){
	       //  using (var objectSpace = action.Application.CreateObjectSpace(typeof(MSAuthentication))){
		      //   var disconnectMicrosoft = action.Controller.Frame.Action<MicrosoftModule>().DisconnectMicrosoft();
		      //   var currentUserId = action.Application.CurrentUserId();
		      //   var objectByKey = objectSpace.GetObjectByKey<MSAuthentication>(currentUserId);
		      //   var userName = objectByKey?.UserName;
		      //   if (!disconnectMicrosoft.Data.ContainsKey("ToolTip")){
			     //    disconnectMicrosoft.Data["ToolTip"] = disconnectMicrosoft.ToolTip;
		      //   }
		      //   disconnectMicrosoft.ToolTip = $"{disconnectMicrosoft.Data["ToolTip"]} {userName}";
	       //  }
        // }

        internal static IObservable<TSource> TraceMicrosoftModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
	        Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
	        [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, MicrosoftModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);

        

        
        [PublicAPI]
        public static IUserRequestBuilder Me(this IBaseRequestBuilder builder) => builder.Client.Me();
        [PublicAPI]
        public static IUserRequestBuilder Me(this IBaseRequest builder) => builder.Client.Me();
        public static IUserRequestBuilder Me(this IBaseClient client) => ((GraphServiceClient)client).Me;
        
        static Prompt ToPrompt(this OAuthPrompt prompt) 
            => prompt switch{
		        OAuthPrompt.Consent => Prompt.Consent,
		        OAuthPrompt.Login => Prompt.ForceLogin,
		        _ => Prompt.SelectAccount
	        };


        static IClientApplicationBase CreateClientApp(this XafApplication application){
	        var modelOAuth = application.Model.OAuthMS();
	        return application.GetPlatform() == Platform.Web
		        ? (IClientApplicationBase) ConfidentialClientApplicationBuilder.Create(modelOAuth.ClientId)
			        .WithRedirectUri(modelOAuth.RedirectUri).WithClientSecret(modelOAuth.ClientSecret).WithAuthority(AuthorityUri).Build()
		        : PublicClientApplicationBuilder.Create(modelOAuth.ClientId).WithAuthority(AuthorityUri)
			        .WithRedirectUri(modelOAuth.RedirectUri).Build();
        }

        [UsedImplicitly]
		public static void Configuration(IAppBuilder app){
			app.SetDefaultSignInAsAuthenticationType(AuthenticationType);
	        app.UseCookieAuthentication(CookieAuthenticationOptions());
	        app.UseOpenIdConnectAuthentication(OpenIdConnectOptions());
        }

		private static CookieAuthenticationOptions CookieAuthenticationOptions() 
            => new CookieAuthenticationOptions{AuthenticationType = AuthenticationType, AuthenticationMode = AuthenticationMode.Passive,
				CookieName = $".AspNet.{AuthenticationType}", ExpireTimeSpan = TimeSpan.FromMinutes(5)
			};

		private static OpenIdConnectAuthenticationOptions OpenIdConnectOptions() 
            => new OpenIdConnectAuthenticationOptions{ Authority = AuthorityUri.ToString(),
				TokenValidationParameters = new TokenValidationParameters{ValidateIssuer = false,ValidateAudience = false},
				Notifications = new OpenIdConnectAuthenticationNotifications{
					RedirectToIdentityProvider = RedirectToIdentityProvider,
					AuthorizationCodeReceived = async notification => await AuthorizationCodeReceived(notification)
				}
			};

		private static async Task AuthorizationCodeReceived(AuthorizationCodeReceivedNotification notification){
			notification.HandleCodeRedemption();
			var application = AppDomain.CurrentDomain.XAF().WebApplication();
			application.AddNonSecuredType(typeof(MSAuthentication));
			var propertiesDictionary = notification.AuthenticationTicket.Properties.Dictionary;
			var userId = TypeDescriptor.GetConverter(typeof(Guid)).ConvertFromString(propertiesDictionary["userid"]);
			var result = await application.AquireTokenByAuthorizationCode(notification.Code, userId);
			if (application.Security.IsSecurityStrategyComplex()){
				await XafApplicationRXExtensions.Logon(application, userId).FirstAsync();
			}

			application.UpdateUserName(userId, result.Account.Username);
			notification.HandleCodeRedemption(null, result.IdToken);
		}

		private static Task RedirectToIdentityProvider(RedirectToIdentityProviderNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> notification){
			var protocolMessage = notification.ProtocolMessage;
			var properties = notification.OwinContext.Authentication.AuthenticationResponseChallenge.Properties.Dictionary;
			protocolMessage.Prompt = properties["prompt"];
			protocolMessage.RedirectUri = properties["RedirectUri"];
			protocolMessage.PostLogoutRedirectUri = protocolMessage.RedirectUri;
			protocolMessage.ClientId = properties["clientId"];
			protocolMessage.Scope = $"openid offline_access {properties["scopes"]}";
			return Task.FromResult(0);
		}

		private static void UpdateUserName(this XafApplication application, object userId, string userName){
			using (var objectSpace = application.CreateObjectSpace(typeof(MSAuthentication))){
				var authentication = objectSpace.GetObjectByKey<MSAuthentication>(userId);
				authentication.UserName = userName;
				objectSpace.CommitChanges();
			}
		}

		private static IObservable<AuthenticationResult> AquireTokenByAuthorizationCode(this XafApplication application, string code, object currentUserId){
			var clientApp = application.CreateClientApp();
			return Observable.FromAsync(() => ((ConfidentialClientApplication) clientApp)
					.AcquireTokenByAuthorizationCode(application.Model.OAuthMS().Scopes(), code)
					.ExecuteAsync())
				.Merge(clientApp.UserTokenCache.SynchStorage(application.CreateObjectSpace, (Guid) currentUserId)
					.IgnoreElements().To<AuthenticationResult>())
				.FirstAsync()
				.TraceMicrosoftModule();
		}

        static readonly Subject<GraphServiceClient> ClientSubject=new Subject<GraphServiceClient>();

        public static IObservable<GraphServiceClient> Client => ClientSubject.AsObservable();

        public static IObservable<(Frame frame, GraphServiceClient client)> AuthorizeMS(this  IObservable<Frame> source,
	        Func<MsalUiRequiredException,IClientApplicationBase,  IObservable<AuthenticationResult>> aquireToken = null) 
            => source.SelectMany(frame => frame.View.AsObjectView().Application().AuthorizeMS(aquireToken)
                    .Select(client => (frame, client)));

		public static IObservable<GraphServiceClient> AuthorizeMS(this XafApplication application, 
			Func<MsalUiRequiredException,IClientApplicationBase,  IObservable<AuthenticationResult>> aquireToken = null) 
            => application.CreateClientApp()
                .Authorize(cache => cache.SynchStorage(application.CreateObjectSpace, application.CurrentUserId()), aquireToken, application)
                .Do(client => {})
                .Do(ClientSubject.OnNext)
        ;

		private static Guid CurrentUserId(this XafApplication application) 
            => application.Security.IsSecurityStrategyComplex() ? (Guid) application.Security.UserId
				: $"{application.Title}{Environment.MachineName}{Environment.UserName}".ToGuid();

		static IObservable<GraphServiceClient> Authorize(this IClientApplicationBase clientApp, Func<ITokenCache, IObservable<TokenCacheNotificationArgs>> storeResults,
			Func<MsalUiRequiredException,IClientApplicationBase, IObservable<AuthenticationResult>> aquireToken, XafApplication application){

			aquireToken ??= ((exception,app) =>Observable.Throw<AuthenticationResult>(new UserFriendlyException("Azure authentication failed. Use the profile view to authenticate again")));
			var authResults = Observable.FromAsync(clientApp.GetAccountsAsync)
				.Select(accounts => accounts.FirstOrDefault())
				.SelectMany(account => Observable.FromAsync(() => clientApp.AcquireTokenSilent(application.Model.OAuthMS().Scopes(), account).ExecuteAsync()))
				.Catch<AuthenticationResult, MsalUiRequiredException>(e => aquireToken(e,clientApp));
			var authenticationResult = storeResults(clientApp.UserTokenCache)
				.Select(args => (AuthenticationResult)null).IgnoreElements()
				.Merge(authResults).FirstAsync();
			return authenticationResult.Select(result => new GraphServiceClient(new DelegateAuthenticationProvider(request => {
				request.Headers.Authorization = new AuthenticationHeaderValue("bearer", result.AccessToken);
				request.Headers.Add("Prefer", "IdType=\"ImmutableId\"");
				return request.ReturnObservable().TraceMicrosoftModule().ToTask();
			})));
		}

		private static IObservable<AuthenticationResult> Challenge(this XafApplication application){
			var modelOAuth = application.Model.OAuthMS();
			var authenticationProperties = new AuthenticationProperties(new Dictionary<string, string>
					{{"userid", application.CurrentUserId().ToString()},{"RedirectUri", modelOAuth.RedirectUri},
						{"prompt", modelOAuth.Prompt.ToString().ToLower()},{"scopes", modelOAuth.Scopes().Join(" ")},
						{"clientId", modelOAuth.ClientId}})
				{RedirectUri = modelOAuth.RedirectUri};
			HttpContext.Current.Response.SuppressFormsAuthenticationRedirect = true;
			HttpContext.Current.GetOwinContext().Authentication.Challenge(authenticationProperties, AuthenticationType);
			return Observable.Empty<AuthenticationResult>();
		}

		public static IObservable<GenericEventArgs<IObservable<AuthenticationResult>>> CustomAquireTokenInteractively 
            => CustomAquireTokenInteractivelySubject.AsObservable();

		private static IObservable<AuthenticationResult> AquireTokenInteractively(this IClientApplicationBase clientApp, XafApplication application){
			var aquireTokenInteractively = HttpContext.Current == null
				? Observable.Defer(() => ((IPublicClientApplication) clientApp).AquireTokenInteractively(application))
				: application.Challenge();
			var args = new GenericEventArgs<IObservable<AuthenticationResult>>(aquireTokenInteractively);
			CustomAquireTokenInteractivelySubject.OnNext(args);
			return args.Instance
				.Do(result => application.UpdateUserName(application.CurrentUserId(),result?.Account.Username))
				.TraceMicrosoftModule(result => result.Account?.Username);
		}
		
		private static IObservable<AuthenticationResult> AquireTokenInteractively(this IPublicClientApplication clientApp, XafApplication application) 
            => clientApp.AcquireTokenInteractive(application.Model.OAuthMS().Scopes()).WithUseEmbeddedWebView(true)
				.WithPrompt(application.Model.OAuthMS().Prompt.ToPrompt()).ExecuteAsync()
                .ToObservable(new SynchronizationContextScheduler(SynchronizationContext.Current));
	}
}