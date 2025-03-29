using System;
using System.Linq;
using System.Net;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.UriExtensions {
    public static partial class UriExtensions {
        public static NetworkCredential SetCredentials(this WebProxy proxy, Uri address) {
            if (string.IsNullOrEmpty(address.UserInfo)) return null;
            proxy.UseDefaultCredentials = false;
            proxy.Address = new Uri(address.Scheme.JoinString("://", address.Host));
            var strings = address.UserInfo.Split(':');
            return new NetworkCredential(strings.First(), strings.Last());
        }
    }
}