using System;
using System.Net;
using System.Net.Http;
using Fasterflect;

namespace Xpand.Extensions.Network {
    public static partial class NetworkExtensions {
        public static IPAddress[] IPAddresses(this Uri uri)
            => Dns.GetHostAddresses(uri.DnsSafeHost);

    }
}