using System.Net.Http.Headers;

namespace Xpand.Extensions.Network {
    public static partial class NetworkExtensions {
        public static void Replace(this HttpHeaders headers, string key, string value) {
            if (headers.Contains(key)) headers.Remove(key);
            headers.Add(key, value);
        }
    }
}