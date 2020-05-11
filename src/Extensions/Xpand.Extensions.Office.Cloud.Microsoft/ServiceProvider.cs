using System.Linq;
using System.Net.Http.Headers;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using Prompt = Microsoft.Identity.Client.Prompt;
using JetBrains.Annotations;
using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using File = System.IO.File;

namespace Xpand.Extensions.Office.Cloud.Microsoft{
    public static class ServiceProvider{
        [PublicAPI]
        public static string AppCredentialsFile = "AzureAppCredentials.json";
        static ServiceProvider(){
            var setupInformation = System.AppDomain.CurrentDomain.SetupInformation;
            var credentialsPath = $@"{setupInformation.PrivateBinPath ?? setupInformation.ApplicationBase}\{AppCredentialsFile}";
            var text = File.ReadAllText(credentialsPath);
            var settings = JsonConvert.DeserializeObject<dynamic>(text);
            ClientAppBuilder = PublicClientApplicationBuilder.Create($"{settings.ClientId}")
                .WithAuthority(AzureCloudInstance.AzurePublic, "common")
                .WithRedirectUri($"{settings.RedirectUrl}");
        }
        public static IUserRequestBuilder Me(this IBaseRequestBuilder builder) => builder.Client.Me();
        public static IUserRequestBuilder Me(this IBaseRequest builder) => builder.Client.Me();
        public static IUserRequestBuilder Me(this IBaseClient client) => ((GraphServiceClient)client).Me;

        public static PublicClientApplicationBuilder ClientAppBuilder { get; }

        public static IObservable<GraphServiceClient> AuthorizeMS(this XafApplication application){
            return ClientAppBuilder.Authorize(cache => cache.SynchStorage(application.CreateObjectSpace, (Guid) SecuritySystem.CurrentUserId));
        }
        public static IObservable<GraphServiceClient> Authorize(this PublicClientApplicationBuilder builder,
            Func<ITokenCache, IObservable<TokenCacheNotificationArgs>> storeResults){

            var clientApp = builder.Build();
            var scopes = new[] { "user.read", "tasks.readwrite", "calendars.readwrite" };
            var authResults = Observable.FromAsync(() => clientApp.GetAccountsAsync())
                .Select(accounts => accounts.FirstOrDefault())
                .SelectMany(account => Observable.FromAsync(() => clientApp.AcquireTokenSilent(scopes, account).ExecuteAsync()))
                .Catch<AuthenticationResult, MsalUiRequiredException>(e => clientApp.AquireTokenInteractively(scopes, null));
            var authenticationResult = storeResults(clientApp.UserTokenCache)
                .Select(args => (AuthenticationResult)null).IgnoreElements()
                .Merge(authResults).FirstAsync();
            return authenticationResult.Select(result => new GraphServiceClient(new DelegateAuthenticationProvider(request => {
                request.Headers.Authorization = new AuthenticationHeaderValue("bearer", result.AccessToken);
                request.Headers.Add("Prefer", "IdType=\"ImmutableId\"");
                return System.Threading.Tasks.Task.FromResult(0);
            })));
        }

        private static IObservable<AuthenticationResult> AquireTokenInteractively(this IPublicClientApplication clientApp, string[] scopes, IAccount account) 
            => Observable.FromAsync(() => clientApp.AcquireTokenInteractive(scopes)
                .WithAccount(account).WithUseEmbeddedWebView(false).WithPrompt(Prompt.SelectAccount).ExecuteAsync());
    }
}