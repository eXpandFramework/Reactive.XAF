using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Drawing;
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
using Owin;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.AppDomainExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Office.Cloud.Microsoft;
using Xpand.XAF.Modules.Office.Cloud.Microsoft.BusinessObjects;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Reactive.Services.Security;
using Prompt = Microsoft.Identity.Client.Prompt;
using Platform = Xpand.Extensions.XAF.XafApplicationExtensions.Platform;

[assembly: OwinStartup(typeof(MicrosoftService))]
namespace Xpand.XAF.Modules.Office.Cloud.Microsoft{
	public static class MicrosoftService{
		static readonly Subject<GenericEventArgs<IObservable<AuthenticationResult>>> CustomAquireTokenInteractivelySubject=new Subject<GenericEventArgs<IObservable<AuthenticationResult>>>();
		private static readonly Uri AuthorityUri=new Uri(string.Format(CultureInfo.InvariantCulture, "https://login.microsoftonline.com/{0}{1}", "common", "/v2.0"));
        private static string _appId ;
        private static string _appSecret ;
        private static string _redirectUri ;
        private static readonly string AuthenticationType=OpenIdConnectAuthenticationDefaults.AuthenticationType;
        

        static MicrosoftService() => 
	        Configure(ConfigurationManager.AppSettings["MSClientID"], ConfigurationManager.AppSettings["RedirectUri"],ConfigurationManager.AppSettings["MSClientSecret"]);

        public static void Configure(string appId,string redirectUrl,string appSecret=null){
	        _appId = appId;
	        _appSecret = appSecret;
	        _redirectUri = redirectUrl;
        }

        public static IObservable<bool> MicrosoftNeedsAuthentication(this XafApplication application) => 
	        application.NewObjectSpace(space => (space.GetObjectByKey<MSAuthentication>( application.CurrentUserId()) == null).ReturnObservable())
		        .SelectMany(b => !b ? application.AuthorizeMS(Observable.Throw<AuthenticationResult>)
				    .To(false).Catch(true.ReturnObservable()) : true.ReturnObservable())
		        .TraceMicrosoftModule();

        public static SimpleAction ConnectMicrosoft(this (MicrosoftModule, Frame frame) tuple) => tuple
	        .frame.Action(nameof(ConnectMicrosoft)).As<SimpleAction>();

        public static SimpleAction DisconnectMicrosoft(this (MicrosoftModule, Frame frame) tuple) => tuple
	        .frame.Action(nameof(DisconnectMicrosoft)).As<SimpleAction>();

        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager){
	        var registerActions = manager.RegisterActions().Publish().RefCount();
	        var whenApplication = manager.WhenApplication(application => registerActions.ExecuteActions().Merge(registerActions.ConfigureStyle())
		        .Merge(application.WhenWeb().CheckAsync(nameof(MicrosoftModule))));

	        return registerActions.ToUnit()
		        .Merge(whenApplication)
		        .ToUnit();
        }

        private static IObservable<Unit> ExecuteActions(this IObservable<SimpleAction> registerActions) =>
	        registerActions.ActivateWhenUserDetails()
		        .SelectMany(action => action.Activate()
		        .Merge(action.Execution())
	        ).ToUnit();

        private static IObservable<SimpleAction> Execution(this SimpleAction action) => action
	        .WhenExecute(e => {
		        var execute = e.Action.Id == nameof(DisconnectMicrosoft)
			        ? e.Action.Application.NewObjectSpace(space => {
				        var objectSpace = e.Action.View().ObjectSpace;
				        objectSpace.Delete(objectSpace.GetObjectByKey<MSAuthentication>(e.Action.Application.CurrentUserId()));
				        objectSpace.CommitChanges();
				        return e.Action.AsSimpleAction().ReturnObservable();
			        })
			        : e.Action.Application.AuthorizeMS().To(e.Action.AsSimpleAction());
		        return execute.ActivateWhenAuthenticationNeeded();
	        })
	        .TraceMicrosoftModule();


        private static IObservable<SimpleAction> Activate(this SimpleAction action) =>
	        action.Application.MicrosoftNeedsAuthentication()
		        .ObserveOn(SynchronizationContext.Current)
		        .Do(b => {
			        action.Active(nameof(MicrosoftNeedsAuthentication), action.Id == nameof(ConnectMicrosoft) ? b : !b);
			        if (!b&& action.Id==nameof(DisconnectMicrosoft)){
						action.UpdateDisconnectActionToolTip();
			        }
		        })
		        .To(action)
		        .TraceMicrosoftModule(a => a.Id)
        ;

        private static IObservable<Unit> ConfigureStyle(this IObservable<SimpleAction> source) => source
	        .WhenCustomizeControl()
	        .Select(_ => {
		        var application = _.sender.Application;
		        if (application.GetPlatform()==Platform.Web){
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
	        })
	        .ToUnit();
        
        private static IObservable<SimpleAction> ActivateWhenUserDetails(this IObservable<SimpleAction> registerActions) =>
	        registerActions.ActivateInUserDetails(true)
		        .Do(action => action.Active(nameof(MicrosoftNeedsAuthentication),false) )
		        .Publish().RefCount();


        private static IObservable<SimpleAction> ActivateWhenAuthenticationNeeded(this IObservable<SimpleAction> source) =>
	        source.SelectMany(action => action.Application.MicrosoftNeedsAuthentication()
			        .ObserveOnWindows(SynchronizationContext.Current)
			        .Do(b => {
				        var actions = action.Controller.Frame.Action<MicrosoftModule>();
				        if (action.Id == nameof(ConnectMicrosoft)){
					        action.Active(nameof(MicrosoftNeedsAuthentication), b);
							actions.DisconnectMicrosoft().Active(nameof(MicrosoftNeedsAuthentication),!b);
				        }
				        else{
					        action.Active(nameof(MicrosoftNeedsAuthentication), !b);
					        actions.ConnectMicrosoft().Active(nameof(MicrosoftNeedsAuthentication),b);
				        }
				        action.UpdateDisconnectActionToolTip();
			        }).To(action))
		        .WhenActive()
		        .TraceMicrosoftModule();

        public static IObservable<T> ObserveOnWindows<T>(this IObservable<T> source,SynchronizationContext synchronizationContext) => 
	        AppDomain.CurrentDomain.IsHosted() ? source : source.ObserveOn(synchronizationContext);

        private static void UpdateDisconnectActionToolTip(this SimpleAction action){
	        using (var objectSpace = action.Application.CreateObjectSpace(typeof(MSAuthentication))){
		        var disconnectMicrosoft = action.Controller.Frame.Action<MicrosoftModule>().DisconnectMicrosoft();
		        var currentUserId = action.Application.CurrentUserId();
		        var objectByKey = objectSpace.GetObjectByKey<MSAuthentication>(currentUserId);
		        var userName = objectByKey?.UserName;
		        disconnectMicrosoft.ToolTip = $"{disconnectMicrosoft.Model.ToolTip} {userName}";
	        }
        }

        internal static IObservable<TSource> TraceMicrosoftModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
	        Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
	        [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) =>
	        source.Trace(name, MicrosoftModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);

        
        private static IObservable<SimpleAction> RegisterActions(this ApplicationModulesManager manager) =>
	        manager.RegisterViewSimpleAction(nameof(ConnectMicrosoft), Initialize)
		        .Merge(manager.RegisterViewSimpleAction(nameof(DisconnectMicrosoft), Initialize));

        private static void Initialize(this SimpleAction action){
	        action.Caption = "Sign in with Microsoft";
	        action.ImageName = "Microsoft";
	        if (action.Id == nameof(ConnectMicrosoft)){
		        action.ToolTip = "Connect";
	        }
	        else{
		        action.Caption = "Sign out Microsoft";
		        action.ToolTip="Sign out";
	        }
	        action.PaintStyle=ActionItemPaintStyle.CaptionAndImage;

        }
        
        [PublicAPI]
        public static IUserRequestBuilder Me(this IBaseRequestBuilder builder) => builder.Client.Me();
        [PublicAPI]
        public static IUserRequestBuilder Me(this IBaseRequest builder) => builder.Client.Me();
        public static IUserRequestBuilder Me(this IBaseClient client) => ((GraphServiceClient)client).Me;
        

        static IClientApplicationBase CreateClientApp(this Platform platform) =>
	        platform==Platform.Web ? (IClientApplicationBase) ConfidentialClientApplicationBuilder.Create(_appId)
			        .WithRedirectUri(_redirectUri).WithClientSecret(_appSecret).WithAuthority(AuthorityUri).Build()
		        : PublicClientApplicationBuilder.Create(_appId).WithAuthority(AuthorityUri).WithRedirectUri(_redirectUri).Build();

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

		private static string _prompt;
		private static string _scopes;

		private static OpenIdConnectAuthenticationOptions OpenIdConnectOptions() =>
			new OpenIdConnectAuthenticationOptions{
				ClientId = _appId, Authority = AuthorityUri.ToString(), 
				RedirectUri = _redirectUri, PostLogoutRedirectUri = _redirectUri,
				TokenValidationParameters = new TokenValidationParameters{ValidateIssuer = false},
				Notifications = new OpenIdConnectAuthenticationNotifications{
					RedirectToIdentityProvider = notification => {
						notification.ProtocolMessage.Prompt = _prompt;
						notification.ProtocolMessage.Scope=$"openid offline_access {_scopes}";
						return Task.FromResult(0);
					},
					AuthorizationCodeReceived = async notification => {
						notification.HandleCodeRedemption();
						var application = AppDomain.CurrentDomain.XAF().WebApplication();
						application.AddNonSecuredType(typeof(MSAuthentication));
						var propertiesDictionary = notification.AuthenticationTicket.Properties.Dictionary;
						var userId = TypeDescriptor.GetConverter(typeof(Guid)).ConvertFromString(propertiesDictionary["userid"]);
						var result = await application.AquireTokenByAuthorizationCode(notification.Code, userId);
						if (application.Security.IsSecurityStrategyComplex()){
							await XafApplicationRXExtensions.Logon(application, userId).FirstAsync();
						}
						application.UpdateUserName( userId, result.Account.Username);
						notification.HandleCodeRedemption(null, result.IdToken);
					}
				}
			};

		private static void UpdateUserName(this XafApplication application, object userId, string userName){
			using (var objectSpace = application.CreateObjectSpace(typeof(MSAuthentication))){
				var authentication = objectSpace.GetObjectByKey<MSAuthentication>(userId);

				authentication.UserName = userName;
				objectSpace.CommitChanges();
			}
		}


		private static Prompt ToPrompt(this OAuthPrompt prompt) =>
			prompt switch{
				OAuthPrompt.Consent => global::Microsoft.Identity.Client.Prompt.Consent,
				OAuthPrompt.Login => global::Microsoft.Identity.Client.Prompt.ForceLogin,
				OAuthPrompt.None => global::Microsoft.Identity.Client.Prompt.Never,
				_ => global::Microsoft.Identity.Client.Prompt.SelectAccount
			};

		private static OAuthPrompt Prompt(this XafApplication application) => 
			application.Model.ToReactiveModule<IModelReactiveModuleOffice>().Office.Microsoft().OAuth.Prompt;

		private static string[] OAuthScopes(this XafApplication xafApplication) => 
			$"{xafApplication.Model.ToReactiveModule<IModelReactiveModuleOffice>().Office.Microsoft().OAuth.Scopes}".Split(' ').Add("User.Read").ToArray();

		private static IObservable<AuthenticationResult> AquireTokenByAuthorizationCode(this XafApplication application, string code, object currentUserId){
			var clientApp = application.GetPlatform().CreateClientApp();
			return Observable.FromAsync(() => ((ConfidentialClientApplication) clientApp)
					.AcquireTokenByAuthorizationCode(application.OAuthScopes(), code)
					.ExecuteAsync())
				.Merge(clientApp.UserTokenCache.SynchStorage(application.CreateObjectSpace, (Guid) currentUserId)
					.IgnoreElements().To<AuthenticationResult>())
				.FirstAsync()
				.TraceMicrosoftModule();
		}

		public static IObservable<GraphServiceClient> AuthorizeMS(this XafApplication application, 
			Func<MsalUiRequiredException,  IObservable<AuthenticationResult>> aquireToken = null) => application.GetPlatform().CreateClientApp()
			.Authorize(cache => cache.SynchStorage(application.CreateObjectSpace, application.CurrentUserId()),
				aquireToken, application)
		;

		private static Guid CurrentUserId(this XafApplication application) =>
			application.Security.IsSecurityStrategyComplex() ? (Guid) application.Security.UserId
				: $"{application.Title}{Environment.MachineName}{Environment.UserName}".ToGuid();

		static IObservable<GraphServiceClient> Authorize(this IClientApplicationBase clientApp,
			Func<ITokenCache, IObservable<TokenCacheNotificationArgs>> storeResults,
			Func<MsalUiRequiredException, IObservable<AuthenticationResult>> aquireToken, XafApplication application){

			aquireToken ??= ((exception) =>clientApp.AquireTokenInteractively(application));
			var authResults = Observable.FromAsync(clientApp.GetAccountsAsync)
				.Select(accounts => accounts.FirstOrDefault())
				.SelectMany(account => Observable.FromAsync(() => clientApp.AcquireTokenSilent(application.OAuthScopes(), account).ExecuteAsync()))
				.Catch<AuthenticationResult, MsalUiRequiredException>(e => aquireToken(e));
			var authenticationResult = storeResults(clientApp.UserTokenCache)
				.Select(args => (AuthenticationResult)null).IgnoreElements()
				.Merge(authResults).FirstAsync();
			return authenticationResult.Select(result => new GraphServiceClient(new DelegateAuthenticationProvider(request => {
				request.Headers.Authorization = new AuthenticationHeaderValue("bearer", result.AccessToken);
				request.Headers.Add("Prefer", "IdType=\"ImmutableId\"");
				return request.ReturnObservable().TraceMicrosoftModule().ToTask();
			})));
		}

		private static IObservable<AuthenticationResult> AquireToken(Guid currentUserId){
			var authenticationProperties = new AuthenticationProperties(new Dictionary<string, string>
					{{"userid", currentUserId.ToString()},{"RedirectUri", HttpContext.Current.Request.Url.ToString()},{"prompt", "consent"}})
				{RedirectUri = HttpContext.Current.Request.Url.ToString()};
			HttpContext.Current.Response.SuppressFormsAuthenticationRedirect = true;
			HttpContext.Current.GetOwinContext().Authentication.Challenge(authenticationProperties, AuthenticationType);
			var webApplication = AppDomain.CurrentDomain.XAF().WebApplication();
			_scopes = webApplication.OAuthScopes().Join(" ");
			_prompt = webApplication.Prompt().ToString().ToLower();
			return Observable.Empty<AuthenticationResult>();
		}

		public static IObservable<GenericEventArgs<IObservable<AuthenticationResult>>> CustomAquireTokenInteractively => 
			CustomAquireTokenInteractivelySubject.AsObservable();

		private static IObservable<AuthenticationResult> AquireTokenInteractively(this IClientApplicationBase clientApp,
			XafApplication application){
			var aquireTokenInteractively = HttpContext.Current == null
				? Observable.Defer(() => ((IPublicClientApplication) clientApp).AcquireTokenInteractive(application.OAuthScopes()).WithUseEmbeddedWebView(true)
					.WithPrompt(application.Prompt().ToPrompt()).ExecuteAsync().ToObservable(new SynchronizationContextScheduler(SynchronizationContext.Current))
					.Do(result => application.UpdateUserName(application.CurrentUserId(),result.Account.Username))
					.Catch<AuthenticationResult, Exception>(Observable.Throw<AuthenticationResult>))
				: AquireToken(application.CurrentUserId());
			var args = new GenericEventArgs<IObservable<AuthenticationResult>>(aquireTokenInteractively);
			CustomAquireTokenInteractivelySubject.OnNext(args);
			return args.Instance.TraceMicrosoftModule(result => result.Account?.Username);
		}
	}
}