using System.Linq;
using System.Net.Http.Headers;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using Prompt = Microsoft.Identity.Client.Prompt;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Web;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Security;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using Xpand.Extensions.Office.Cloud.Microsoft;
using Xpand.Extensions.ProcessExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using File = System.IO.File;
using Process = System.Diagnostics.Process;

[assembly: OwinStartup(typeof(ServiceProvider))]
namespace Xpand.Extensions.Office.Cloud.Microsoft{
	public static class ServiceProvider{
        [PublicAPI]
        public static string AppCredentialsFile = "AzureAppCredentials.json";
        private static readonly Uri AuthorityUri=new Uri(string.Format(CultureInfo.InvariantCulture, "https://login.microsoftonline.com/{0}{1}", "common", "/v2.0"));
        private static readonly string AppId ;
        private static readonly string AppSecret ;
        private static readonly string RedirectUri ;
        private static readonly string AuthenticationType=OpenIdConnectAuthenticationDefaults.AuthenticationType;
        private const string GraphScopes = "User.Read Calendars.ReadWrite Tasks.ReadWrite";

        static ServiceProvider(){
	        var setupInformation = AppDomain.CurrentDomain.SetupInformation;
	        var credentialsPath = $@"{setupInformation.PrivateBinPath ?? setupInformation.ApplicationBase}\{AppCredentialsFile}";
	        if (File.Exists(credentialsPath)){
		        var text = File.ReadAllText(credentialsPath);
		        var settings = JsonConvert.DeserializeObject<dynamic>(text);
		        AppId = settings.MSClientId;
		        AppSecret = settings.MSAppSecret;
		        RedirectUri = settings.RedirectUri;
	        }
        }

        public static SimpleAction AuthenticateMS(this (MicrosoftModule, Frame frame) tuple) => tuple
	        .frame.Action(nameof(AuthenticateMS)).As<SimpleAction>();

        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager){
	        var registerAction = manager.RegisterAction();
	        return registerAction.ToUnit()
		        .Merge(manager.WhenApplication(application => application.ReactiveModulesModel().OfficeModel().MicrosoftModel()
			        .SelectMany(microsoft => registerAction.ActivateInUserDetails()
				        .ConfigureCloudAction<MSAuthentication>(microsoft.DisconnectImageName,microsoft.ConnectImageName,
					        action => action.Application.AuthorizeMS().Select(client => client).ToUnit(),
					        action => action.Application.AuthorizeMS((exception, strings) => Observable.Throw<AuthenticationResult>(exception)).ToUnit())
				        .ToUnit())))
		    .ToUnit();
        }

        private static IObservable<SimpleAction> RegisterAction(this ApplicationModulesManager manager) =>
	        manager.RegisterViewSimpleAction(nameof(AuthenticateMS), action => {
		        action.Caption = "Microsoft";
		        action.TargetViewType = ViewType.DetailView;
	        }).Publish().RefCount();
        
        [PublicAPI]
        public static IUserRequestBuilder Me(this IBaseRequestBuilder builder) => builder.Client.Me();
        [PublicAPI]
        public static IUserRequestBuilder Me(this IBaseRequest builder) => builder.Client.Me();
        public static IUserRequestBuilder Me(this IBaseClient client) => ((GraphServiceClient)client).Me;

        static IClientApplicationBase CreateClientApp() =>
			HttpContext.Current!=null? (IClientApplicationBase) ConfidentialClientApplicationBuilder.Create(AppId)
					.WithRedirectUri(RedirectUri).WithClientSecret(AppSecret).WithAuthority(AuthorityUri).Build()
				:PublicClientApplicationBuilder.Create(AppId).WithAuthority(AuthorityUri).WithRedirectUri(RedirectUri).Build();

        [UsedImplicitly]
		public static void Configuration(IAppBuilder app){
			app.SetDefaultSignInAsAuthenticationType(AuthenticationType);
	        app.UseCookieAuthentication(CookieAuthenticationOptions());
	        app.UseOpenIdConnectAuthentication(OpenIdConnectOptions());
        }

		private static CookieAuthenticationOptions CookieAuthenticationOptions() =>
			new CookieAuthenticationOptions{AuthenticationType = AuthenticationType, AuthenticationMode = AuthenticationMode.Passive,
				CookieName = $".AspNet.{AuthenticationType}", ExpireTimeSpan = TimeSpan.FromMinutes(5)
			};

		private static OpenIdConnectAuthenticationOptions OpenIdConnectOptions() =>
			new OpenIdConnectAuthenticationOptions{
				ClientId = AppId, Authority = AuthorityUri.ToString(), Scope = $"openid offline_access {GraphScopes}",
				RedirectUri = RedirectUri, PostLogoutRedirectUri = RedirectUri,
				TokenValidationParameters = new TokenValidationParameters{ValidateIssuer = false},
				Notifications = new OpenIdConnectAuthenticationNotifications{
					AuthorizationCodeReceived = async notification => {
						var propertiesDictionary = notification.AuthenticationTicket.Properties.Dictionary;
						if (Process.GetCurrentProcess().IsUnderIISExpress()){
							notification.HandleCodeRedemption();
							var application = (XafApplication)HttpContext.Current.Session["SessionApplicationVariable"];
							var applicationSecurity = application.Security as SecurityStrategy;
							applicationSecurity?.AnonymousAllowedTypes.Add(typeof(MSAuthentication));
							var userId = TypeDescriptor.GetConverter(SecuritySystem.UserType.ToTypeInfo().KeyMember.MemberType)
								.ConvertFromString(propertiesDictionary["userid"]);
							var result = await application.AquireTokenByAuthrizationCode(notification.Code, userId);
							await application.Logon(userId).FirstAsync();
							notification.HandleCodeRedemption(null, result.IdToken);
						}
						else{
							propertiesDictionary.Add("code", notification.Code);
						}
					}
				}
			};

		private static IObservable<AuthenticationResult> AquireTokenByAuthrizationCode(this XafApplication application, string code, object currentUserId){
			var clientApp = CreateClientApp();
			return Observable.FromAsync(() => ((ConfidentialClientApplication) clientApp)
					.AcquireTokenByAuthorizationCode(GraphScopes.Split(' '), code)
					.ExecuteAsync())
				.Merge(clientApp.UserTokenCache.SynchStorage(application.CreateObjectSpace, (Guid) currentUserId)
					.IgnoreElements().To<AuthenticationResult>())
				.FirstAsync();
		}

		public static IObservable<GraphServiceClient> AuthorizeMS(this XafApplication application, 
			Func<MsalUiRequiredException, string[], IObservable<AuthenticationResult>> aquireToken = null) => CreateClientApp()
			.Authorize(cache => cache.SynchStorage(application.CreateObjectSpace, (Guid) SecuritySystem.CurrentUserId), aquireToken);

		static IObservable<GraphServiceClient> Authorize(this IClientApplicationBase clientApp,
			Func<ITokenCache, IObservable<TokenCacheNotificationArgs>> storeResults, Func<MsalUiRequiredException, string[], IObservable<AuthenticationResult>> aquireToken){

			aquireToken ??= ((exception, strings) =>clientApp.AquireTokenInteractively(strings));
			var authResults = Observable.FromAsync(clientApp.GetAccountsAsync)
				.Select(accounts => accounts.FirstOrDefault())
				.SelectMany(account => Observable.FromAsync(() => clientApp.AcquireTokenSilent(GraphScopes.Split(' '), account).ExecuteAsync()))
				.Catch<AuthenticationResult, MsalUiRequiredException>(e => aquireToken(e,GraphScopes.Split(' ')));
			var authenticationResult = storeResults(clientApp.UserTokenCache)
				.Select(args => (AuthenticationResult)null).IgnoreElements()
				.Merge(authResults).FirstAsync();
			return authenticationResult.Select(result => new GraphServiceClient(new DelegateAuthenticationProvider(request => {
				request.Headers.Authorization = new AuthenticationHeaderValue("bearer", result.AccessToken);
				request.Headers.Add("Prefer", "IdType=\"ImmutableId\"");
				return Task.FromResult(0);
			})));
		}

		private static IObservable<AuthenticationResult> AquireToken(){
			var authenticationProperties = new AuthenticationProperties(new Dictionary<string, string>
					{{"userid", SecuritySystem.CurrentUserId.ToString()},{"RedirectUri", HttpContext.Current.Request.Url.ToString()}})
				{RedirectUri = HttpContext.Current.Request.Url.ToString()};
			HttpContext.Current.GetOwinContext().Authentication.Challenge(authenticationProperties, AuthenticationType);
			return Observable.Empty<AuthenticationResult>();
		}

		private static IObservable<AuthenticationResult> AquireTokenInteractively(this IClientApplicationBase clientApp, string[] scopes) 
			=>HttpContext.Current==null? Observable.FromAsync(() => ((IPublicClientApplication) clientApp).AcquireTokenInteractive(scopes)
				.WithUseEmbeddedWebView(true).WithPrompt(Prompt.SelectAccount).ExecuteAsync()).Catch<AuthenticationResult,Exception>(Observable.Throw<AuthenticationResult>):AquireToken();

    }
}