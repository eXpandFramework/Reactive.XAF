using System;

namespace Xpand.Extensions.DateTimeExtensions{
    public static class DateTimeExtensions{
        public static DateTime UnixTimestampToDateTimeMilliSecond(this double unixTime) => UnixTimestampToDateTime(unixTime, TimeSpan.TicksPerMillisecond);

        private static DateTime UnixTimestampToDateTime(double unixTime, long ticks){
            var unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var unixTimeStampInTicks = (long) (unixTime * ticks);
            return new DateTime(unixStart.Ticks + unixTimeStampInTicks, DateTimeKind.Utc);
        }

        public static double UnixTimestampFromDateTimeMilliseconds(this DateTime dateTime) => (dateTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
    }
}