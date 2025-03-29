using System;
using System.Linq;

namespace Xpand.Extensions.UriExtensions {
    public static partial class UriExtensions {
        public static Uri GetRootUri(this Uri url) => new($"{url.Scheme}://{new Uri(url.WithoutQuery()).GetRootDomain()}");

        public static string GetRootDomain(this Uri url) {
            var host = url.Host.Split('.');
            return host.Length < 2
                ? string.Join(".", host)
                : string.Join(".", host.Skip(host.Length - 2));
        }
        
        public static string WithoutQuery(this Uri uri) {
            var s = uri.ToString();
            var i = s.IndexOf('?');
            return i >= 0 ? s.Substring(0, i) : s;
        }
    }
}