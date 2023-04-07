using System;
using System.Collections.Specialized;
using Cysharp.Text;

namespace Xpand.Extensions.LinqExtensions {
    public static partial class LinqExtensions {
        public static string ToQueryString(this NameValueCollection nvc) {
            if (nvc == null) return string.Empty;
            using var sb = ZString.CreateUtf8StringBuilder();
            foreach (string key in nvc.Keys) {
                if (string.IsNullOrWhiteSpace(key)) continue;
                var values = nvc.GetValues(key);
                if (values == null) continue;
                foreach (string value in values) {
                    sb.Append(sb.Length == 0 ? "?" : "&");
                    sb.AppendFormat("{0}={1}", Uri.EscapeDataString(key), Uri.EscapeDataString(value));
                }
            }
            return sb.ToString();
        }
    }
}