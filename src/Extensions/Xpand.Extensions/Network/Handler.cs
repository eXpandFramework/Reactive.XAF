using System.Net.Http;
using Fasterflect;
using HarmonyLib;
using Microsoft.AspNetCore.Http;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.Network {
    public static partial class NetworkExtensions {

        public static bool IsJsonResponse(this HttpResponseMessage message) => message.Content.Headers.ContentType?.MediaType == "application/json";

        public static string GetFullUrl(this HttpRequest httpRequest) {
            var path = new[]{httpRequest.Scheme,"//",httpRequest.Host.ToUriComponent(),httpRequest.PathBase.ToUriComponent(),httpRequest.Path.ToUriComponent()};
            return !httpRequest.QueryString.HasValue ? path.JoinString() : LinqExtensions.LinqExtensions.Join(path.AddItem(httpRequest.QueryString.Value));
        }


        public static DelegatingHandler Handler(this HttpClient client)
            => (DelegatingHandler)HandlerAccessor(client);
        public static T Handler<T>(this HttpClient client) where T:DelegatingHandler
            => (T)client.Handler();
        
        private static readonly MemberGetter HandlerAccessor=typeof(HttpClient).DelegateForGetFieldValue("_handler");

    }
    
    
}