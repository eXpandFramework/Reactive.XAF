using System;

namespace Xpand.Extensions.DateTimeExtensions {
    public static partial class DateTimeExtensions {
        public static DateTime UnixTimeStampToDateTime(this long unixTimeStamp) 
            => DateTimeOffset.FromUnixTimeMilliseconds(unixTimeStamp).UtcDateTime;
        private const decimal TicksPerNanosecond = TimeSpan.TicksPerMillisecond / 1000m / 1000;
        public static DateTime UnixNanoSecondsTimeStampToDateTime(this long unixTimeStamp) 
            => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks((long)Math.Round(unixTimeStamp * TicksPerNanosecond));
    }
}