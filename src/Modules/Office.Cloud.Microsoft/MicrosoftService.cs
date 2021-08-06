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
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.XAF.AppDomainExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.SecurityExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Office.Cloud.Microsoft;
using Xpand.XAF.Modules.Office.Cloud.Microsoft.BusinessObjects;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using AuthenticationException = System.Security.Authentication.AuthenticationException;
using Platform = Xpand.Extensions.XAF.XafApplicationExtensions.Platform;
using Prompt = Microsoft.Identity.Client.Prompt;

[assembly: OwinStartup(typeof(MicrosoftService))]
namespace Xpand.XAF.Modules.Office.Cloud.Microsoft{
	public static class MicrosoftService{
		
		private static readonly Uri AuthorityUri=new(string.Format(CultureInfo.InvariantCulture, "https://login.microsoftonline.com/{0}{1}", "common", "/v2.0"));
        
        private static readonly string AuthenticationType=OpenIdConnectAuthenticationDefaults.AuthenticationType;

        public static IObservable<bool> MicrosoftNeedsAuthentication(this XafApplication application) 
            => application.NeedsAuthentication<MSAuthentication>(() 
                => application.AuthorizeMS((_, _) => Observable.Throw<AuthenticationResult>(new AuthenticationException(nameof(MicrosoftService))))
	                .Catch<GraphServiceClient,AuthenticationException>(_ => Observable.Empty<GraphServiceClient>())
	                .To(false).SwitchIfEmpty(true.ReturnObservable()))
                .TraceMicrosoftModule();

        public static SimpleAction ConnectMicrosoft(this (MicrosoftModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(ConnectMicrosoft)).As<SimpleAction>();

        public static SimpleAction DisconnectMicrosoft(this (MicrosoftModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(DisconnectMicrosoft)).As<SimpleAction>();

        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) 
            => manager.Connect("Microsoft", typeof(MSAuthentication), application 
                => application.MicrosoftNeedsAuthentication(), application 
                => application.AuthorizeMS((_, app) => app.AcquireTokenInteractively(application)).ToUnit());

        internal static IObservable<TSource> TraceMicrosoftModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
	        Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
	        [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, MicrosoftModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName);
        
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
		        ? ConfidentialClientApplicationBuilder.Create(modelOAuth.ClientId)
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
            => new() {AuthenticationType = AuthenticationType, AuthenticationMode = AuthenticationMode.Passive,
				CookieName = $".AspNet.{AuthenticationType}", ExpireTimeSpan = TimeSpan.FromMinutes(5)
			};

		private static OpenIdConnectAuthenticationOptions OpenIdConnectOptions() 
            => new() { 
				ResponseType = OpenIdConnectResponseType.CodeIdToken,
                Scope = OpenIdConnectScope.OpenIdProfile,
                Authority = AuthorityUri.ToString(),
				TokenValidationParameters = new TokenValidationParameters{ValidateIssuer = false,ValidateAudience = false},
				Notifications = new OpenIdConnectAuthenticationNotifications{
					RedirectToIdentityProvider = RedirectToIdentityProvider,
					AuthorizationCodeReceived = async notification => await AuthorizationCodeReceived(notification),
					AuthenticationFailed = async _ => await Task.CompletedTask
				}
			};

		private static async Task AuthorizationCodeReceived(AuthorizationCodeReceivedNotification notification){
			notification.HandleCodeRedemption();
			var application = AppDomain.CurrentDomain.XAF().WebApplication();
			application.AddNonSecuredType(typeof(MSAuthentication));
			var propertiesDictionary = notification.AuthenticationTicket.Properties.Dictionary;
			var userId = TypeDescriptor.GetConverter(typeof(Guid)).ConvertFromString(propertiesDictionary["userid"]);
			var result = await application.AcquireTokenByAuthorizationCode(notification.Code, userId);
			if (application.Security.IsSecurityStrategyComplex()){
				await application.Logon(userId).FirstAsync();
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
			using var objectSpace = application.CreateObjectSpace(typeof(MSAuthentication));
			var authentication = objectSpace.GetObjectByKey<MSAuthentication>(userId);
			authentication.UserName = userName;
			objectSpace.CommitChanges();
		}

		private static IObservable<AuthenticationResult> AcquireTokenByAuthorizationCode(this XafApplication application, string code, object currentUserId){
			var clientApp = application.CreateClientApp();
			return Observable.FromAsync(() => ((ConfidentialClientApplication) clientApp)
					.AcquireTokenByAuthorizationCode(application.Scopes(), code)
					.ExecuteAsync())
				.Merge(clientApp.UserTokenCache.SynchStorage(application.CreateObjectSpace, (Guid) currentUserId)
					.IgnoreElements().To<AuthenticationResult>())
				.FirstAsync()
				.TraceMicrosoftModule(result => result.Account?.Username);
		}

		private static string[] Scopes(this XafApplication application) => application.Model.OAuthMS().Scopes().Concat("User.Read".YieldItem()).ToArray();

		static readonly Subject<GraphServiceClient> ClientSubject=new();
		public static IObservable<GraphServiceClient> Client => ClientSubject.AsObservable();

        public static IObservable<(Frame frame, GraphServiceClient client)> AuthorizeMS(this  IObservable<Frame> source,
	        Func<MsalUiRequiredException,IClientApplicationBase,  IObservable<AuthenticationResult>> acquireToken = null) 
            => source.SelectMany(frame => Observable.Defer(() => frame.Application.MicrosoftNeedsAuthentication().WhenDefault()
                .SelectMany(_=>frame.View.AsObjectView().Application().AuthorizeMS(acquireToken)
                    .Select(client => (frame, client)))).ObserveOn(SynchronizationContext.Current));

		public static IObservable<GraphServiceClient> AuthorizeMS(this XafApplication application, 
			Func<MsalUiRequiredException,IClientApplicationBase,  IObservable<AuthenticationResult>> acquireToken = null) 
            => application.CreateClientApp()
                .Authorize(cache => cache.SynchStorage(application.CreateObjectSpace, application.CurrentUserId()), acquireToken, application)
                .Do(ClientSubject.OnNext)
        ;

		private static Guid CurrentUserId(this XafApplication application) 
            => application.Security.IsSecurityStrategyComplex() ? (Guid) application.Security.UserId
				: $"{application.Title}{Environment.MachineName}{Environment.UserName}".ToGuid();

		static IObservable<GraphServiceClient> Authorize(this IClientApplicationBase clientApp, Func<ITokenCache, IObservable<TokenCacheNotificationArgs>> storeResults,
			Func<MsalUiRequiredException,IClientApplicationBase, IObservable<AuthenticationResult>> acquireToken, XafApplication application){

			acquireToken ??= ((_,_) =>Observable.Throw<AuthenticationResult>(new UserFriendlyException("Azure authentication failed. Use the profile view to authenticate again")));
			var authResults = Observable.FromAsync(clientApp.GetAccountsAsync)
				.Select(accounts => accounts.FirstOrDefault())
				.SelectMany(account => Observable.FromAsync(() => clientApp.AcquireTokenSilent(application.Scopes(), account).ExecuteAsync()))
				.Catch<AuthenticationResult, MsalUiRequiredException>(e => acquireToken(e,clientApp));
			var authenticationResult = storeResults(clientApp.UserTokenCache)
				.Select(_ => (AuthenticationResult)null).IgnoreElements()
				.Merge(authResults).FirstAsync();
			return authenticationResult.Select(result => new GraphServiceClient(new DelegateAuthenticationProvider(request => {
				request.Headers.Authorization = new AuthenticationHeaderValue("bearer", result.AccessToken);
				request.Headers.Add("Prefer", "IdType=\"ImmutableId\"");
				return request.ReturnObservable()
                    .TraceMicrosoftModule()
                    .ToTask();
			})));
		}

		private static IObservable<AuthenticationResult> Challenge(this XafApplication application){
			var modelOAuth = application.Model.OAuthMS();
			var authenticationProperties = new AuthenticationProperties(new Dictionary<string, string>
					{{"userid", application.CurrentUserId().ToString()},{"RedirectUri", modelOAuth.RedirectUri},
						{"prompt", modelOAuth.Prompt.ToString().ToLower()},{"scopes", application.Scopes().Join(" ")},
						{"clientId", modelOAuth.ClientId}})
				{RedirectUri = modelOAuth.RedirectUri};
			HttpContext.Current.Response.SuppressFormsAuthenticationRedirect = true;
			HttpContext.Current.GetOwinContext().Authentication.Challenge(authenticationProperties, AuthenticationType);
			return Observable.Empty<AuthenticationResult>();
		}

		public static IObservable<GenericEventArgs<IObservable<AuthenticationResult>>> CustomAcquireTokenInteractively 
            => CustomAcquireTokenInteractivelySubject.AsObservable();
        static readonly Subject<GenericEventArgs<IObservable<AuthenticationResult>>> CustomAcquireTokenInteractivelySubject=new();

		private static IObservable<AuthenticationResult> AcquireTokenInteractively(this IClientApplicationBase clientApp, XafApplication application){
			var acquireTokenInteractively = HttpContext.Current == null
				? Observable.Defer(() => ((IPublicClientApplication) clientApp).AcquireTokenInteractively(application))
				: application.Challenge();
			var args = new GenericEventArgs<IObservable<AuthenticationResult>>(acquireTokenInteractively);
			CustomAcquireTokenInteractivelySubject.OnNext(args);
			return args.Instance
				.Do(result => application.UpdateUserName(application.CurrentUserId(),result?.Account.Username))
				.TraceMicrosoftModule(result => result.Account?.Username);
		}
		
		private static IObservable<AuthenticationResult> AcquireTokenInteractively(this IPublicClientApplication clientApp, XafApplication application) 
            => clientApp.AcquireTokenInteractive(application.Scopes()).WithUseEmbeddedWebView(true)
				.WithPrompt(application.Model.OAuthMS().Prompt.ToPrompt()).ExecuteAsync()
                .ToObservable(new SynchronizationContextScheduler(SynchronizationContext.Current));
	}
}