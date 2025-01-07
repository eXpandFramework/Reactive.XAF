using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Xpand.Extensions.Numeric;

namespace Xpand.Extensions.Network {
    public static partial class NetworkExtensions {
        private static readonly Random Random = new(DateTime.Now.Millisecond);
        public static int GetRandomAvailablePort(this IPEndPoint[] endPoints, int startRange = 1024, int endRange = 49151)
            => startRange.Range(endRange - startRange).ToArray().OrderBy(_ => Random.Next()).ToArray()
                .First(port => endPoints.All(endPoint => endPoint.Port != port));

        public static int FindFreePort(this AppDomain appDomain) {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try {
                var localIP = new IPEndPoint(IPAddress.Any, 0);
                socket.Bind(localIP);
                return ((IPEndPoint) socket.LocalEndPoint ?? throw new Exception("Not found free port.")).Port;
            }
            finally {
                socket.Close();
            }
        }

    }
}