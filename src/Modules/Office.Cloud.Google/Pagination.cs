using System;
using System.Net;
using System.Reactive.Linq;
using Fasterflect;
using Google;
using Google.Apis.Requests;
using Xpand.Extensions.Office.Cloud.BusinessObjects;

namespace Xpand.XAF.Modules.Office.Cloud.Google{
    public static class Pagination{
        public static IObservable<TResponse> List<TResponse, TMaxResult>(this IClientServiceRequest<TResponse> listRequest,
            TMaxResult maxResults, ITokenStore tokenStore = null, Action<ITokenStore> saveToken = null, Func<GoogleApiException, bool> repeat = null){

            listRequest.SetPropertyValue("MaxResults", maxResults);
            repeat ??= (exception => exception.HttpStatusCode == HttpStatusCode.Gone);
            if (tokenStore != null){
                tokenStore.EntityName = typeof(TResponse).FullName;
                listRequest.SetPropertyValue("SyncToken", tokenStore.Token);
            }
            var events = Observable.FromAsync(() => listRequest.ExecuteAsync())
                .Catch<TResponse, GoogleApiException>(e => {
                    if (tokenStore != null) tokenStore.Token = null;
                    return repeat(e) ? listRequest.List(maxResults, tokenStore, saveToken, repeat) : Observable.Throw<TResponse>(e);
                })
                .Do(_ => listRequest.SetPropertyValue("PageToken", _.GetPropertyValue("NextPageToken")))
                .Repeat().TakeUntil(_ => listRequest.GetPropertyValue("PageToken") == null)
                .Finally(() => saveToken?.Invoke(tokenStore))
                .Publish().RefCount();

            if (tokenStore != null){
                return events.Where(_ => _.GetPropertyValue("NextSyncToken") != null).LastOrDefaultAsync().Do(_ => {
                    if (_ != null) tokenStore.Token = $"{_.GetPropertyValue("NextSyncToken")}";
                }).IgnoreElements().Merge(events);
            }

            return events;
        }


    }
}