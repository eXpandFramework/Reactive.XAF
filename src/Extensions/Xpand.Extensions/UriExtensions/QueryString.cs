using System;
using System.Collections.Specialized;
using System.Web;

namespace Xpand.Extensions.UriExtensions {
    public static partial class UriExtensions {
        public static NameValueCollection QueryString(this Uri uri) => HttpUtility.ParseQueryString(uri?.Query!);
    }
}