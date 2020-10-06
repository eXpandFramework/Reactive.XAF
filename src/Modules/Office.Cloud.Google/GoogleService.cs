using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Fasterflect;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2.Web;
using Google.Apis.Requests;
using Google.Apis.Services;
using JetBrains.Annotations;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.SecurityExtensions;
using Xpand.Extensions.XAF.ViewExtenions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Office.Cloud.Google.BusinessObjects;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Platform = Xpand.Extensions.XAF.XafApplicationExtensions.Platform;



namespace Xpand.XAF.Modules.Office.Cloud.Google{
    public static class GoogleService{
        public static IObservable<(Frame frame, UserCredential userCredential)> AuthorizeGoogle(this IObservable<Frame> source) 
            => source.SelectMany(frame => frame.Application.GoogleNeedsAuthentication().WhenDefault()
	            .SelectMany(_ => frame.View.AsObjectView().Application().AuthorizeGoogle()
		            .Select(userCredential => (frame, userCredential))));

        public static IObservable<bool> GoogleNeedsAuthentication(this XafApplication application) 
            => application.NeedsAuthentication<GoogleAuthentication>(() => Observable.Using(application.GoogleAuthorizationCodeFlow,
                flow => flow.LoadTokenAsync(application.CurrentUserId().ToString(), CancellationToken.None).ToObservable()
                    .WhenNotDefault().RefreshToken(flow, application.CurrentUserId().ToString()))
                .SwitchIfEmpty(true.ReturnObservable()))
                .Publish().RefCount()
                .TraceGoogleModule();

        [PublicAPI]
        public static SimpleAction ConnectGoogle(this (GoogleModule googleModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(ConnectGoogle)).As<SimpleAction>();

        [PublicAPI]
        public static SimpleAction DisconnectGoogle(this (GoogleModule googleModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(DisconnectGoogle)).As<SimpleAction>();

        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) 
            => manager.Connect("Google", typeof(GoogleAuthentication), application
                => application.GoogleNeedsAuthentication(), application
                => application.AuthorizeGoogle((exception, flow) => flow.AuthorizeApp(application)).ToUnit())
                .Merge(manager.ExchangeCodeForToken());

        internal static IObservable<TSource> TraceGoogleModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
	        Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
	        [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, GoogleModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);
        
        private static IObservable<bool> RefreshToken(this IObservable<TokenResponse> source, GoogleAuthorizationCodeFlow flow, string userId) 
            => source.SelectMany(response 
                => response.IsExpired(flow.Clock) ? flow.RefreshTokenAsync(userId, response.RefreshToken, CancellationToken.None).ToObservable().Select(tokenResponse 
                    => tokenResponse.IsExpired(flow.Clock)) : false.ReturnObservable())
                .TraceGoogleModule();


        private class AuthorizationCodeFlow : GoogleAuthorizationCodeFlow{
	        public AuthorizationCodeFlow(Initializer initializer) : base(initializer){
	        }

	        public override AuthorizationCodeRequestUrl CreateAuthorizationCodeRequest(string redirectUri) 
                => new GoogleAuthorizationCodeRequestUrl(new Uri(AuthorizationServerUrl)) {
			        ClientId = ClientSecrets.ClientId,
			        Scope = string.Join(" ", Scopes),
			        RedirectUri = redirectUri,
			        AccessType = "offline",Prompt = Prompt
		        };
        };
        
        private static AuthorizationCodeFlow GoogleAuthorizationCodeFlow(this XafApplication application)
            => new AuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer{
                ClientSecrets = application.NewClientSecrets(), Scopes = application.Model.OAuthGoogle().Scopes(),
                DataStore = application.NewXafOAuthDataStore(),Prompt = application.Model.OAuthGoogle().Prompt.ToString().ToLower()
            });


        static ClientSecrets NewClientSecrets(this XafApplication application){
            var oAuth = application.Model.OAuthGoogle();
            return new ClientSecrets(){ClientId = oAuth.ClientId,ClientSecret = oAuth.ClientSecret};
        }

        static IObservable<Unit> ExchangeCodeForToken(this ApplicationModulesManager manager) 
            => manager.WhenApplication(application => application.WhenWindowCreated().When(TemplateContext.ApplicationWindow)
                .SelectMany(window => {
                    var httpContext = AppDomain.CurrentDomain.Web().HttpContext();
                    var code = httpContext?.GetPropertyValue("Request").GetIndexer("code");
                    if (code != null){
                        var uri = httpContext.GetPropertyValue("Request").GetPropertyValue("Url").ToString();
                        var state = httpContext?.GetPropertyValue("Request").GetIndexer("state").ToString();
                        var tokenResponse = window.Application.GoogleAuthorizationCodeFlow().ExchangeCodeForTokenAsync(
                            window.Application.CurrentUserId().ToString(), code.ToString(), uri.Substring(0, uri.IndexOf("?", StringComparison.Ordinal)), CancellationToken.None).Result;
                        return window.Application.WhenWeb()
                            .TraceGoogleModule(response => $"IdToken={tokenResponse.IdToken}")
                            .Do(api => api.Redirect(state.Substring(0, state.Length - AuthorizationCodeWebApp.StateRandomLength),false)).To((default(TokenResponse)));
                    }
                    return Observable.Empty<TokenResponse>();
                })).ToUnit();

        [PublicAPI]
        public static IObservable<T> NewService<T>(this IObservable<UserCredential> source) where T : BaseClientService 
            => source.Select(NewService<T>);

        public static T NewService<T>(this UserCredential credential) where T : BaseClientService 
            => (T) typeof(T).CreateInstance(new BaseClientService.Initializer(){HttpClientInitializer = credential});

        public static IObservable<UserCredential> AuthorizeGoogle(this XafApplication application,Func<UserFriendlyException,IAuthorizationCodeFlow,  IObservable<UserCredential>> aquireToken = null) 
            => application.GoogleAuthorizationCodeFlow().AuthorizeGoogle(application,aquireToken)
                .TraceGoogleModule(credential => credential.UserId);

        public static IObservable<TResponse> ToObservable<TResponse>(this ClientServiceRequest request) => ((IClientServiceRequest<TResponse>)request).ToObservable();

        public static IObservable<TResponse> ToObservable<TResponse>(this IClientServiceRequest<TResponse> request) => request.ExecuteAsync().ToObservable();

        static readonly Subject<GenericEventArgs<Func<XafApplication,XafOAuthDataStore>>> CustomizeOathDataStoreSubject=new Subject<GenericEventArgs<Func<XafApplication, XafOAuthDataStore>>>();
        [PublicAPI]
        public static IObservable<GenericEventArgs<Func<XafApplication, XafOAuthDataStore>>> CustomizeOathDataStore => CustomizeOathDataStoreSubject.AsObservable();

        static XafOAuthDataStore NewXafOAuthDataStore(this XafApplication application){
            var cloudTypes = XafTypesInfo.Instance.PersistentTypes.Where(info => info.Type.Namespace == typeof(CloudOfficeObject).Namespace)
                .Select(info => info.Type);
            application.Security.AddAnonymousType(cloudTypes.ToArray());
            var args = new GenericEventArgs<Func<XafApplication, XafOAuthDataStore>>();
            CustomizeOathDataStoreSubject.OnNext(args);
            return args.Handled ? args.Instance(application) : new XafOAuthDataStore(application.CreateObjectSpace, application.CurrentUserId(),application.GetPlatform());
        }
        
        public static IObservable<GenericEventArgs<IObservable<UserCredential>>> CustomAquireTokenInteractively 
            => CustomAquireTokenInteractivelySubject.AsObservable();
        static readonly Subject<GenericEventArgs<IObservable<UserCredential>>> CustomAquireTokenInteractivelySubject=new Subject<GenericEventArgs<IObservable<UserCredential>>>();

        static IObservable<UserCredential> AuthorizeGoogle(this GoogleAuthorizationCodeFlow flow, XafApplication application,
	        Func<UserFriendlyException, IAuthorizationCodeFlow, IObservable<UserCredential>> aquireToken) 
	        => flow.RequestAuthorizationCode(application,aquireToken);

        private static IObservable<UserCredential> AuthorizeApp(this IAuthorizationCodeFlow flow, XafApplication application) 
	        => Observable.Defer(() => ((XafOAuthDataStore) flow.DataStore).Platform == Platform.Win
			        ? flow.AuthorizeInstalledApp(application) : flow.AuthorizeWebApp(application));

        private static IObservable<UserCredential> AuthorizeInstalledApp(this IAuthorizationCodeFlow flow, XafApplication application) 
            => Observable.FromAsync(() => new AuthorizationCodeInstalledApp(flow, new LocalServerCodeReceiver()).AuthorizeAsync(application.CurrentUserId().ToString(), CancellationToken.None));

        private static IObservable<UserCredential> AuthorizeWebApp(this  IAuthorizationCodeFlow flow,XafApplication application){
	        var redirectUri = AppDomain.CurrentDomain.Web().HttpContext().GetPropertyValue("Request").GetPropertyValue("Url").CallMethod("GetLeftPart",UriPartial.Path).ToString() ;
                return Observable.Empty<UserCredential>();
	        }
	        return new AuthorizationCodeWebApp(flow, redirectUri, redirectUri)
                .AuthorizeAsync(application.CurrentUserId().ToString(), CancellationToken.None)
		        .ToObservable()
		        .SelectMany(result => result.RedirectUri == null ? result.Credential.ReturnObservable() : application.WhenWeb()
                    .TraceGoogleModule(api => redirectUri)
                        .Do(api => api.Redirect(result.RedirectUri,false)).To(default(UserCredential)))
                .WhenNotDefault();
        }

        static IObservable<UserCredential> RequestAuthorizationCode(this IAuthorizationCodeFlow flow, XafApplication application,
	        Func<UserFriendlyException, IAuthorizationCodeFlow, IObservable<UserCredential>> aquireToken) 
	        => Observable.FromAsync(() => flow.LoadTokenAsync(application.CurrentUserId().ToString(), CancellationToken.None))
		        .Select(token => flow.ShouldForceTokenRetrieval() || token == null || token.RefreshToken == null && token.IsExpired(flow.Clock))
		        .SelectMany(b => b ? Observable.Throw<UserCredential>(new UserFriendlyException("Google authentication failed. Use the profile view to authenticate again"))
			        : default(UserCredential).ReturnObservable())
		        .Catch<UserCredential,UserFriendlyException>(exception => {
			        var args = new GenericEventArgs<IObservable<UserCredential>>(aquireToken(exception, flow));
			        CustomAquireTokenInteractivelySubject.OnNext(args);
			        return args.Instance;
		        })
		        .SelectMany(_ => flow.AuthorizeApp(application));
    }
}