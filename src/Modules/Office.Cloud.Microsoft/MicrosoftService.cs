using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Templates;
using Fasterflect;
using JetBrains.Annotations;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Newtonsoft.Json;
using Owin;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.ProcessExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Office.Cloud.Microsoft;
using Xpand.XAF.Modules.Office.Cloud.Microsoft.BusinessObjects;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Prompt = Microsoft.Identity.Client.Prompt;
using File = System.IO.File;
using Platform = Xpand.Extensions.XAF.XafApplicationExtensions.Platform;
using Process = System.Diagnostics.Process;

[assembly: OwinStartup(typeof(MicrosoftService))]
namespace Xpand.XAF.Modules.Office.Cloud.Microsoft{
	public static class MicrosoftService{
        [PublicAPI]
        public static string AppCredentialsFile = "AzureAppCredentials.json";
        private static readonly Uri AuthorityUri=new Uri(string.Format(CultureInfo.InvariantCulture, "https://login.microsoftonline.com/{0}{1}", "common", "/v2.0"));
        private static readonly string AppId ;
        private static readonly string AppSecret ;
        private static readonly string RedirectUri ;
        private static readonly string AuthenticationType=OpenIdConnectAuthenticationDefaults.AuthenticationType;
        private const string GraphScopes = "User.Read Calendars.ReadWrite Tasks.ReadWrite";

        static MicrosoftService(){
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

        public static IObservable<bool> MicrosoftNeedsAuthentication(this XafApplication application) => 
	        application.NewObjectSpace(space => (space.GetObjectByKey<MSAuthentication>( SecuritySystem.CurrentUserId) == null).ReturnObservable())
		        .SelectMany(b => !b ? application.AuthorizeMS((e, strings) => Observable.Throw<AuthenticationResult>(e))
				    .To(false).Catch(true.ReturnObservable()) : true.ReturnObservable());

        public static SimpleAction ConnectMicrosoft(this (MicrosoftModule, Frame frame) tuple) => tuple
	        .frame.Action(nameof(ConnectMicrosoft)).As<SimpleAction>();
        public static SimpleAction DisconnectMicrosoft(this (MicrosoftModule, Frame frame) tuple) => tuple
	        .frame.Action(nameof(DisconnectMicrosoft)).As<SimpleAction>();

        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager){
	        var registerActions = manager.RegisterActions().Publish().RefCount();
	        var whenApplication = manager.WhenApplication(application => registerActions.ExecuteActions());
	        return registerActions.ToUnit()
		        .Merge(whenApplication)
		        .ToUnit();
        }

        private static IObservable<Unit> ExecuteActions(this IObservable<SimpleAction> registerActions) =>
	        registerActions.ActivateWhenUserDetails().SelectMany(action => action.Activation().Merge(action.Execution())).ToUnit();

        private static IObservable<SimpleAction> Execution(this SimpleAction action) =>
	        action.WhenExecute().SelectMany(e => e.Action.Id == nameof(DisconnectMicrosoft)
			        ? e.Action.Application.NewObjectSpace(space => {
				        var objectSpace = e.Action.View().ObjectSpace;
				        objectSpace.Delete(objectSpace.GetObjectByKey<MSAuthentication>(SecuritySystem.CurrentUserId));
				        objectSpace.CommitChanges();
				        return e.Action.AsSimpleAction().ReturnObservable();
			        })
			        : e.Action.Application.AuthorizeMS().To(e.Action.AsSimpleAction()))
		        .ActivateWhenAuthenticationNeeded();

        private static IObservable<SimpleAction> Activation(this SimpleAction action) =>
	        action.Application.MicrosoftNeedsAuthentication().ObserveOn(SynchronizationContext.Current)
		        .Do(b => action.Active[nameof(MicrosoftNeedsAuthentication)] = action.Id == nameof(ConnectMicrosoft) ? b : !b).To(action)
		        .ConfigureStyle()
		        .TraceMicrosoftModule(a => $"{a.Id}, {action.Active}");

        private static IObservable<SimpleAction> ConfigureStyle(this IObservable<SimpleAction> source) => source
	        .WhenCustomizeControl()
	        .Select(_ => {
		        if (_.sender.Application.GetPlatform()==Platform.Web){
			        if (_.sender.Id == nameof(ConnectMicrosoft)){
						_.sender.Model.SetValue("IsPostBackRequired",true);
			        }
			        var menuItem = _.e.Control.GetPropertyValue("MenuItem");
			        var itemStyle = menuItem.GetPropertyValue("ItemStyle");
			        itemStyle.GetPropertyValue("Paddings").SetPropertyValue("Padding", new System.Web.UI.WebControls.Unit(2));
			        itemStyle.SetPropertyValue("ImageSpacing", new System.Web.UI.WebControls.Unit(7));
			        itemStyle.GetPropertyValue("Font").SetPropertyValue("Name", "Roboto Medium");
			        itemStyle.GetPropertyValue("SelectedStyle").SetPropertyValue("BackColor", Color.White);
			        itemStyle.SetPropertyValue("ForeColor", ColorTranslator.FromHtml("#757575"));
			        itemStyle.GetPropertyValue("HoverStyle").SetPropertyValue("BackColor", Color.White);
			        menuItem.CallMethod("ForceMenuRendering");
		        }
		        return _.sender;
	        });
        
        private static IObservable<SimpleAction> ActivateWhenUserDetails(this IObservable<SimpleAction> registerActions) =>
	        registerActions.ActivateInUserDetails()
		        .Do(action => action.Active[nameof(MicrosoftNeedsAuthentication)] = false)
		        .Publish().RefCount();


        private static IObservable<SimpleAction> ActivateWhenAuthenticationNeeded(this IObservable<SimpleAction> source) =>
	        source.SelectMany(action => action.Application.MicrosoftNeedsAuthentication()
			        .ObserveOn(SynchronizationContext.Current)
			        .Do(b => {
				        if (action.Id == nameof(ConnectMicrosoft)){
					        action.Active[nameof(MicrosoftNeedsAuthentication)] = !b;
					        action.Controller.Frame.Action<MicrosoftModule>().DisconnectMicrosoft().Active[nameof(MicrosoftNeedsAuthentication)]=b;
				        }
				        else{
					        action.Active[nameof(MicrosoftNeedsAuthentication)] = !b;
					        action.Controller.Frame.Action<MicrosoftModule>().ConnectMicrosoft().Active[nameof(MicrosoftNeedsAuthentication)]=b;
				        }
			        }).To(action))
		        .WhenActive()
		        .TraceMicrosoftModule();

        internal static IObservable<TSource> TraceMicrosoftModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
	        Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
	        [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
	        source.Trace(name, MicrosoftModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);

        
        private static IObservable<SimpleAction> RegisterActions(this ApplicationModulesManager manager) =>
	        manager.RegisterViewSimpleAction(nameof(ConnectMicrosoft), Initialize)
		        .Merge(manager.RegisterViewSimpleAction(nameof(DisconnectMicrosoft), Initialize));

        private static void Initialize(SimpleAction action){
	        action.Caption = "Microsoft";
			action.PaintStyle=ActionItemPaintStyle.CaptionAndImage;
	        action.TargetViewType = ViewType.DetailView;
	        action.ImageName = "Microsoft";
        }
        
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

		static readonly Subject<GenericEventArgs<IObservable<AuthenticationResult>>> CustomAquireTokenInteractivelySubject=new Subject<GenericEventArgs<IObservable<AuthenticationResult>>>();

		public static IObservable<GenericEventArgs<IObservable<AuthenticationResult>>> CustomAquireTokenInteractively => CustomAquireTokenInteractivelySubject.AsObservable();

		private static IObservable<AuthenticationResult> AquireTokenInteractively(this IClientApplicationBase clientApp, string[] scopes){
			var aquireTokenInteractively = HttpContext.Current == null
				? Observable.FromAsync(() => ((IPublicClientApplication) clientApp).AcquireTokenInteractive(scopes)
						.WithUseEmbeddedWebView(true).WithPrompt(Prompt.SelectAccount).ExecuteAsync())
					.Catch<AuthenticationResult, Exception>(Observable.Throw<AuthenticationResult>)
				: AquireToken();
			var args = new GenericEventArgs<IObservable<AuthenticationResult>>(aquireTokenInteractively);
			CustomAquireTokenInteractivelySubject.OnNext(args);
			return args.Instance;
		}
	}
}