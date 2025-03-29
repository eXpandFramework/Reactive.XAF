using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Network;

namespace Xpand.Extensions.UriExtensions {
    public static partial class UriExtensions {
        static readonly HashSet<string> AllHosts= ["localhost", "127.0.0.1", "::1", AppDomain.CurrentDomain.LocalIPAddress()];
        static readonly HashSet<string> Hosts= ["localhost", "127.0.0.1", "::1", AppDomain.CurrentDomain.LocalIPAddress()];
        public static bool IsLocalHost(this Uri uri,bool includeLocalIP=false) 
            => uri.HostNameType == UriHostNameType.IPv4 || uri.HostNameType == UriHostNameType.IPv6
                ? (includeLocalIP?AllHosts:Hosts).Contains(uri.Host) : uri.IsLoopback;
    }
}