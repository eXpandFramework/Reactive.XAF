using System;
using System.Net;
using System.Net.Sockets;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.Network {
    public static partial class NetworkExtensions {
        static string _localIPAddress;
        public static string LocalIPAddress(this AppDomain domain) 
            => _localIPAddress ??= Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(
                address => address.AddressFamily == AddressFamily.InterNetwork,
                () => "No network adapters with an IPv4 address in the system!").ToString();
    }
}