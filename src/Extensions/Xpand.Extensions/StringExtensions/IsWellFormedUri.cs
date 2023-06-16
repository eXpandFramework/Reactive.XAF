using System;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static bool IsWellFormedUri(this string address, UriKind uriKind = UriKind.RelativeOrAbsolute) 
            => Uri.IsWellFormedUriString(address, uriKind);
    }
}