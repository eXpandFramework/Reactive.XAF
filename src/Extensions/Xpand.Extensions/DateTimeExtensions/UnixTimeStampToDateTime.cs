using System;

namespace Xpand.Extensions.DateTimeExtensions {
    public static partial class DateTimeExtensions {
        public static DateTime UnixTimeStampToDateTime(this long unixTimeStamp) 
            => new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(unixTimeStamp).ToLocalTime();
    }
}