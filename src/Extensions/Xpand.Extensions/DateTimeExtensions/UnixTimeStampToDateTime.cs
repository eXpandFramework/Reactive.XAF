using System;

namespace Xpand.Extensions.DateTimeExtensions {
    public static partial class DateTimeExtensions {
        public static DateTime UnixTimeStampToDateTime(this long unixTimeStamp) 
            => DateTimeOffset.FromUnixTimeMilliseconds(unixTimeStamp).UtcDateTime;
    }
}