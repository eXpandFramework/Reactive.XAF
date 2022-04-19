using System;
using System.Net;
using System.Net.Sockets;

namespace Xpand.Extensions.Network {
    public static partial class NetworkExtensions {
        public static string LocalIPAddress(this AppDomain domain){
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList) {
                if (ip.AddressFamily == AddressFamily.InterNetwork) {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

    }
}