using System;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.UriExtensions {
    public static partial class UriExtensions {
        public static Uri Merge(this Uri baseUri, Uri relativeUri) {
            if (relativeUri.OriginalString == String.Empty) return baseUri;
            var uri = new Uri(new Uri(baseUri.ToString().EnsureEndWith("/")), relativeUri);
            return new UriBuilder(baseUri) { Path = uri.AbsolutePath,Query = uri.Query }.Uri;
        }
    }
}