using System;

namespace Xpand.Extensions.DateTimeExtensions {
    public static partial class DateTimeExtensions {
        public static TimeSpan TimeSpan(this long ticks) 
            => System.TimeSpan.FromTicks(ticks);
        
        public static DateTime UnixMillisecondsToDateTime(this long unixTimeStamp) 
            => DateTimeOffset.FromUnixTimeMilliseconds(unixTimeStamp).DateTime;
        public static DateTime UnixSecondsToDateTime(this long unixTimeStamp) 
            => DateTimeOffset.FromUnixTimeSeconds(unixTimeStamp).DateTime;
        
        private const decimal TicksPerNanosecond = System.TimeSpan.TicksPerMillisecond / 1000m / 1000;
        public static DateTime UnixNanoSecondsTimeStampToDateTime(this long unixTimeStamp) 
            => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks((long)Math.Round(unixTimeStamp * TicksPerNanosecond));
    }
}