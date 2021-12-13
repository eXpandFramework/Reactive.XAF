using System;
using System.Collections.Specialized;
using System.Web;

namespace Xpand.Extensions.UriExtensions {
    public static partial class UriExtensions {
        public static Uri ModifyQuery(this Uri uri, Action<NameValueCollection> modify) {
            var queryString = HttpUtility.ParseQueryString(uri?.Query!);
            modify(queryString);
            return new UriBuilder(uri!){
                Query = queryString.ToString()!
            }.Uri;
        }

    }
}