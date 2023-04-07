using System.Net.Http;
using Fasterflect;
using HarmonyLib;
using Microsoft.AspNetCore.Http;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.Network {
    public static partial class NetworkExtensions {
        public static string GetFullUrl(this HttpRequest httpRequest) {
            var path = new[]{httpRequest.Scheme,"//",httpRequest.Host.ToUriComponent(),httpRequest.PathBase.ToUriComponent(),httpRequest.Path.ToUriComponent()};
            return !httpRequest.QueryString.HasValue ? path.JoinConcat() : path.AddItem("?").AddItem(httpRequest.QueryString.Value).JoinConcat();
        }


        public static HttpMessageHandler Handler(this HttpClient client)
            => (HttpMessageHandler)client.GetFieldValue("_handler");
        public static T Handler<T>(this HttpClient client) where T:HttpMessageHandler
            => (T)client.GetFieldValue("_handler");
        
    }
}