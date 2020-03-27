using System;

namespace Xpand.Extensions.DateTime{
    public static class DateTimeExtensions{
        public static System.DateTime UnixTimestampToDateTimeMilliSecond(this double unixTime){
            return UnixTimestampToDateTime(unixTime, TimeSpan.TicksPerMillisecond);
        }

        private static System.DateTime UnixTimestampToDateTime(double unixTime, long ticks){
            var unixStart = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var unixTimeStampInTicks = (long) (unixTime * ticks);
            return new System.DateTime(unixStart.Ticks + unixTimeStampInTicks, DateTimeKind.Utc);
        }

        public static double UnixTimestampFromDateTimeMilliseconds(this System.DateTime dateTime){
            return (dateTime - new System.DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }
    }
}