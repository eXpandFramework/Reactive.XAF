using System;
using System.Net;

namespace Xpand.Extensions.Network {
    public static partial class NetworkExtensions {
        public static IPAddress[] IPAddresses(this Uri uri)
            => Dns.GetHostAddresses(uri.DnsSafeHost);
        
    }
}