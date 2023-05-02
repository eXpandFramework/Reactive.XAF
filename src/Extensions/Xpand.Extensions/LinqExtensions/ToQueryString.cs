using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;


namespace Xpand.Extensions.LinqExtensions {
    public static partial class LinqExtensions {
        public static string ToQueryString(this NameValueCollection nvc) {
            if (nvc == null) return string.Empty;
            var queryParameters = new List<string>();
            foreach (string key in nvc.Keys) {
                if (string.IsNullOrWhiteSpace(key)) continue;
                var values = nvc.GetValues(key);
                if (values == null) continue;
                queryParameters.AddRange(values.Select(value => Uri.EscapeDataString(key).JoinString("=", Uri.EscapeDataString(value))));
            }
            return queryParameters.Count == 0 ? string.Empty : queryParameters.Join("&");
        }
    }
}