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


        public static HttpMessageHandler Handler(this HttpClient client)
            => (HttpMessageHandler)client.GetFieldValue("_handler");
        public static T Handler<T>(this HttpClient client) where T:HttpMessageHandler
            => (T)client.GetFieldValue("_handler");
        
    }
}