using System;                             
using System.Net;
using System.Reactive.Linq;
using Fasterflect;
using Google;
using Google.Apis.Requests;
using Xpand.Extensions.Office.Cloud.BusinessObjects;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.Office.Cloud.Google{
    public static class Pagination{
        public static IObservable<TResponse[]> List<TResponse, TMaxResult>(this IClientServiceRequest<TResponse> request,
            TMaxResult maxResults, ICloudOfficeToken cloudOfficeToken = null, Action<ICloudOfficeToken> saveToken = null, Func<GoogleApiException, bool> repeat = null){

            request.SetPropertyValue("MaxResults", maxResults);
            repeat ??= (exception => exception.HttpStatusCode == HttpStatusCode.Gone);
            if (cloudOfficeToken != null){
                request.SetPropertyValue("SyncToken", cloudOfficeToken.Token);
            }
            var allEvents = Observable.FromAsync(() => request.ExecuteAsync())
                .Catch<TResponse, GoogleApiException>(e => {
                    if (cloudOfficeToken != null) cloudOfficeToken.Token = null;
                    return repeat(e) ? request.List(maxResults, cloudOfficeToken, saveToken, repeat).SelectMany(responses => responses) : Observable.Throw<TResponse>(e);
                })
                .Do(_ => request.SetPropertyValue("PageToken", _.GetPropertyValue("NextPageToken")))
                .Repeat().TakeUntil(_ => request.GetPropertyValue("PageToken") == null)
                .Finally(() => saveToken?.Invoke(cloudOfficeToken))
                .Publish().RefCount();

            if (cloudOfficeToken != null){
                allEvents= allEvents.Where(_ => _.GetPropertyValue("NextSyncToken") != null).LastOrDefaultAsync().Do(_ => {
                    if (_ != null) cloudOfficeToken.Token = $"{_.GetPropertyValue("NextSyncToken")}";
                }).IgnoreElements().Merge(allEvents);
            }

            return allEvents.BufferUntilCompleted().Select(responses => responses);
        }


    }
}