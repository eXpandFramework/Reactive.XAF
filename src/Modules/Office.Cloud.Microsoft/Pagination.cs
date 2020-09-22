using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Web;
using Fasterflect;
using Microsoft.Graph;
using Xpand.Extensions.Office.Cloud.BusinessObjects;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.ReflectionExtensions;

namespace Xpand.XAF.Modules.Office.Cloud.Microsoft{
    public static class Pagination{
        public static IObservable<Entity[]> ListAllItems(this IBaseRequestBuilder builder, Action<ICloudOfficeToken> saveToken = null, ICloudOfficeToken cloudOfficeToken = null) 
            => builder.ListAllItems<Entity>(cloudOfficeToken, saveToken);

        public static IObservable<TEntity[]> ListAllItems<TEntity>(this IBaseRequest request,
            ICloudOfficeToken cloudOfficeToken = null, Action<ICloudOfficeToken> saveToken = null, Func<TEntity, bool> filter = null) where TEntity : Entity{
            filter ??= (_ => true);
            var methodInfo = request.GetType().Methods(Flags.InstancePublic, "GetAsync").First();
            return Observable.FromAsync(() => methodInfo.InvokeAsync(request)).Cast<IEnumerable<TEntity>>()
                .SelectMany(entities => entities.ListAllItems(cloudOfficeToken, saveToken, filter).ToEnumerable().ToArray().ToObservable());
        }

        public static IObservable<TEntity[]> ListAllItems<TEntity>(this IEnumerable<TEntity> page,
            ICloudOfficeToken cloudOfficeToken = null, Action<ICloudOfficeToken> saveToken = null, Func<TEntity, bool> filter = null) where TEntity : Entity{
            filter ??= (_ => true);
            var methodInfo = page.GetType().GetProperty("NextPageRequest")?.PropertyType.Methods(Flags.InstancePublic, "GetAsync").First();
            if (methodInfo == null){
                throw new NotSupportedException($"Pagination not supported for {page.GetType().FullName}");
            }
            var nextPageRequestDelegate = page.GetType().DelegateForGetPropertyValue("NextPageRequest");
            page.AdditionalData().SaveToken( cloudOfficeToken, saveToken);
            var invokeNextPage = Observable.FromAsync(() => methodInfo.InvokeAsync(nextPageRequestDelegate(page)))
                .Select(entities => {
                    page = (IEnumerable<TEntity>)entities;
                    entities.AdditionalData().SaveToken( cloudOfficeToken, saveToken);
                    return entities;
                })
                .Select(o => ((IEnumerable)o).Cast<TEntity>().ToArray());
            return Observable.While(() => nextPageRequestDelegate(page) != null, invokeNextPage).StartWith(page.ToArray())
                .Select(entities => entities.Where(filter).ToArray())
                .SwitchIfEmpty(Enumerable.Empty<TEntity>().ToArray().ReturnObservable());
        }

        private static IDictionary<string, object> AdditionalData(this object page) 
            => (IDictionary<string, object>) page.GetPropertyValue("AdditionalData");

        private static void SaveToken(this IDictionary<string, object> additionalData, ICloudOfficeToken cloudOfficeToken, Action<ICloudOfficeToken> saveToken){
            if (cloudOfficeToken != null){
                if (additionalData.ContainsKey("@odata.deltaLink")){
                    cloudOfficeToken.Token = HttpUtility
                        .ParseQueryString(new Uri(additionalData["@odata.deltaLink"].ToString()).Query).Get("$deltatoken");
                    cloudOfficeToken.TokenType = "deltatoken";
                }
                else if (additionalData.ContainsKey("@odata.nextLink")){
                    cloudOfficeToken.Token = HttpUtility.ParseQueryString(new Uri(additionalData["@odata.nextLink"].ToString()).Query)
                        .Get("$skiptoken");
                    cloudOfficeToken.TokenType = "skiptoken";
                }

                saveToken?.Invoke(cloudOfficeToken);
            }
        }

        public static IObservable<TEntity[]> ListAllItems<TEntity>(this IBaseRequestBuilder builder,
            ICloudOfficeToken cloudOfficeToken = null, Action<ICloudOfficeToken> saveToken = null, Func<TEntity, bool> filter = null) where TEntity : Entity{
            var request = (IBaseRequest)builder.CallMethod("Request");
            return request.ListAllItems(cloudOfficeToken, saveToken, filter);
        }


    }
}