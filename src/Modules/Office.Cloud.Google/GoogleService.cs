using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Web;
using DevExpress.Data.Filtering.Helpers;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2.Web;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.Office.Cloud;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.SecurityExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.Office.Cloud.Google.BusinessObjects;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Reactive.Services.Security;
using Platform = Xpand.Extensions.XAF.XafApplicationExtensions.Platform;



namespace Xpand.XAF.Modules.Office.Cloud.Google{
	public static class GoogleService{
        public static IObservable<bool> GoogleNeedsAuthentication(this XafApplication application) 
            => application.NeedsAuthentication<GoogleAuthentication>(() => Observable.Using(application.GoogleAuthorizationCodeFlow,
                flow => Observable.FromAsync(() => flow.LoadTokenAsync(SecuritySystem.CurrentUserId.ToString(), CancellationToken.None))
                    .RemoveInvalidAuthentication(application).WhenNotDefault().NeedsAuthorization(flow)).ToUnit());

        public static SimpleAction ConnectGoogle(this (GoogleModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(ConnectGoogle)).As<SimpleAction>();

        public static SimpleAction DisconnectGoogle(this (GoogleModule, Frame frame) tuple) 
            => tuple.frame.Action(nameof(DisconnectGoogle)).As<SimpleAction>();

        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) 
            => manager.Connect("Google", typeof(GoogleAuthentication), application
                => application.GoogleNeedsAuthentication(), application
                => application.AuthorizeGoogle().ToUnit());

        internal static IObservable<TSource> TraceGoogleModule<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
	        Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.All,
	        [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
            => source.Trace(name, GoogleModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);

        
		private static Guid CurrentUserId(this XafApplication application) 
            => application.Security.IsSecurityStrategyComplex() ? (Guid) application.Security.UserId
				: $"{application.Title}{Environment.MachineName}{Environment.UserName}".ToGuid();

        private static IObservable<bool> NeedsAuthorization(this IObservable<TokenResponse> source,global::Google.Apis.Auth.OAuth2.Flows.GoogleAuthorizationCodeFlow flow) 
            => source.SelectMany(response => response.IsExpired(flow.Clock) ? flow.RefreshTokenAsync(SecuritySystem.CurrentUserId.ToString(),
			        response.RefreshToken, CancellationToken.None).ToObservable()
		        .Select(tokenResponse => tokenResponse.IsExpired(flow.Clock)) : false.ReturnObservable());

        private static IObservable<TokenResponse> RemoveInvalidAuthentication(this IObservable<TokenResponse> source, XafApplication application) 
            => source.Do(response => {
		        if (response == null){
			        using (var objectSpace = application.CreateObjectSpace()){
				        objectSpace.Delete(objectSpace.GetObjectByKey<GoogleAuthentication>(SecuritySystem.CurrentUserId));
				        objectSpace.CommitChanges();
			        }
		        }
	        });

        private class AuthorizationCodeFlow : global::Google.Apis.Auth.OAuth2.Flows.GoogleAuthorizationCodeFlow{
	        public AuthorizationCodeFlow(Initializer initializer) : base(initializer){
	        }

	        public override AuthorizationCodeRequestUrl CreateAuthorizationCodeRequest(string redirectUri) 
                => new GoogleAuthorizationCodeRequestUrl(new Uri(AuthorizationServerUrl)) {
			        ClientId = ClientSecrets.ClientId,
			        Scope = string.Join(" ", Scopes),
			        RedirectUri = redirectUri,
			        AccessType = "offline",Prompt = "consent"
		        };
        };
        
        private static AuthorizationCodeFlow GoogleAuthorizationCodeFlow(this XafApplication application) 
            => new AuthorizationCodeFlow(new global::Google.Apis.Auth.OAuth2.Flows.GoogleAuthorizationCodeFlow.Initializer{
                ClientSecrets = application.NewClientSecrets(), Scopes = application.Model.OAuthGoogle().Scopes(),
                DataStore = application.NewXafOAuthDataStore()
            });

        static ClientSecrets NewClientSecrets(this XafApplication application){
            var oAuth = application.Model.OAuthGoogle();
            return new ClientSecrets(){ClientId = oAuth.ClientId,ClientSecret = oAuth.ClientSecret};
        }

        internal static IObservable<TokenResponse> AuthorizeGoogle(this IObservable<Window> source) 
            => source.SelectMany(window => {
                var code = HttpContext.Current.Request["code"];
                if (code != null){
                    var uri = HttpContext.Current.Request.Url.ToString();
                    var state = HttpContext.Current.Request["state"];
                    window.Application.GoogleAuthorizationCodeFlow().ExchangeCodeForTokenAsync(
                        window.Application.CurrentUserId().ToString(), code, uri.Substring(0, uri.IndexOf("?", StringComparison.Ordinal)), CancellationToken.None).Wait();
                    return window.Application.WhenWeb()
                        .Do(api => api.Redirect(state.Substring(0, state.Length - AuthorizationCodeWebApp.StateRandomLength))).To((default(TokenResponse)));
                }

                return Observable.Empty<TokenResponse>();
            });

        public static IObservable<UserCredential> AuthorizeGoogle(this XafApplication application) 
            => application.GoogleAuthorizationCodeFlow().AuthorizeGoogle(application);

        static readonly Subject<GenericEventArgs<Func<XafApplication,XafOAuthDataStore>>> CustomizeOathDataStoreSubject=new Subject<GenericEventArgs<Func<XafApplication, XafOAuthDataStore>>>();

        public static IObservable<GenericEventArgs<Func<XafApplication, XafOAuthDataStore>>> CustomizeOathDataStore => CustomizeOathDataStoreSubject.AsObservable();

        static XafOAuthDataStore NewXafOAuthDataStore(this XafApplication application){
            var cloudTypes = XafTypesInfo.Instance.PersistentTypes.Where(info => info.Type.Namespace == typeof(CloudOfficeObject).Namespace)
                .Select(info => info.Type);
            application.Security.AddAnonymousType(cloudTypes.ToArray());
            var args = new GenericEventArgs<Func<XafApplication, XafOAuthDataStore>>();
            CustomizeOathDataStoreSubject.OnNext(args);
            return args.Handled ? args.Instance(application) : new XafOAuthDataStore(application.CreateObjectSpace, (Guid) SecuritySystem.CurrentUserId,application.GetPlatform());
        }
        
        static IObservable<UserCredential> AuthorizeGoogle(this  global::Google.Apis.Auth.OAuth2.Flows.GoogleAuthorizationCodeFlow flow,XafApplication application) 
            => ((XafOAuthDataStore) flow.DataStore).Platform==Platform.Win?Observable.FromAsync(() => GoogleWebAuthorizationBroker.AuthorizeAsync(
                    application.NewClientSecrets(), EmptyEnumerable<string>.Instance, application.CurrentUserId().ToString(), CancellationToken.None, flow.DataStore))
                : flow.AuthorizeWebApp(application);

        private static IObservable<UserCredential> AuthorizeWebApp(this  global::Google.Apis.Auth.OAuth2.Flows.GoogleAuthorizationCodeFlow flow,XafApplication application){
	        var redirectUri = HttpContext.Current.Request.Url.ToString();
	        return new AuthorizationCodeWebApp(flow, redirectUri, redirectUri)
                .AuthorizeAsync(application.CurrentUserId().ToString(), CancellationToken.None)
		        .ToObservable()
		        .SelectMany(result => result.RedirectUri == null
                    ? result.Credential.ReturnObservable() : application.WhenWeb().Do(api => api.Redirect(result.RedirectUri)).To(default(UserCredential)))
                .WhenNotDefault();
        }

	}
}