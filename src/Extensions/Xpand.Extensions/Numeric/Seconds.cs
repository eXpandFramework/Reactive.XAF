using System;

namespace Xpand.Extensions.Numeric {
    public static partial class NumericExtensions {
        public static TimeSpan ToMinutes(this int minutes) => TimeSpan.FromMinutes(minutes);
        public static TimeSpan Minutes(this int minutes) => TimeSpan.FromMinutes(minutes);
        public static TimeSpan ToSeconds(this int seconds) => TimeSpan.FromSeconds(seconds);
        public static TimeSpan Seconds(this int seconds) => TimeSpan.FromSeconds(seconds);
        public static TimeSpan Seconds(this double seconds) => TimeSpan.FromSeconds(seconds);
        public static TimeSpan Hours(this int hours) => TimeSpan.FromHours(hours);
        public static TimeSpan Days(this int hours) => TimeSpan.FromDays(hours);
    }
}