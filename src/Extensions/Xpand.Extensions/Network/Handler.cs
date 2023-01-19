using System.Net.Http;
using Fasterflect;

namespace Xpand.Extensions.Network {
    public static partial class NetworkExtensions {
        public static HttpMessageHandler Handler(this HttpClient client)
            => (HttpMessageHandler)client.GetFieldValue("_handler");
        public static T Handler<T>(this HttpClient client) where T:HttpMessageHandler
            => (T)client.GetFieldValue("_handler");
        
    }
}