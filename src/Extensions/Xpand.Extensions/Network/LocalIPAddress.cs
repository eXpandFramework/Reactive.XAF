using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.Network {
    public static partial class NetworkExtensions {
        static string _localIPAddress;
        public static string LocalIPAddress(this AppDomain domain) 
            => _localIPAddress ??= Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(
                address => address.AddressFamily == AddressFamily.InterNetwork,
                () => "No network adapters with an IPv4 address in the system!").ToString();
        
        public static bool IsSameTo(this Uri first, string second)
            => first.IsSameTo(new Uri(second));
        public static bool IsSameTo(this Uri first, Uri second) 
            => first != null && second != null && string.Equals(first.AbsoluteUri.TrimEnd('/'),
                second.AbsoluteUri.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
    }
}