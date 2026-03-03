using System;

namespace Xpand.Extensions.GuidExtensions {
    public static partial class GuidExtensions {
        public static string ToShortGuid(this Guid guid)
            => guid.ToString("N").Substring(0, 8);
    }
}