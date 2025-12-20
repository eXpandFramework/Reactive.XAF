using System;

namespace Xpand.Extensions.DateTimeExtensions {
    public static partial class DateTimeExtensions {
        public static bool? IsPast(this DateTime dt)
            =>dt==DateTime.MinValue?null: dt < DateTime.Now;
        public static bool? IsFuture(this DateTime dt)
            =>dt==DateTime.MinValue?null: dt > DateTime.Now;
        
        public static bool IsWithinTomorrow(this DateTime dt)
            => !dt.IsWithinADay()&& dt.IsWithinHours(48);
        
        public static bool IsWithinADay(this DateTime dt)
            => dt.IsWithinHours(24);
        
        
        public static bool IsWithinHours(this DateTime date, double hours)
            => date.IsWithin(System.TimeSpan.FromHours(hours));
        
        public static bool IsWithin(this DateTime date, TimeSpan timeSpan)
            => date >= DateTime.Now - timeSpan && date <= DateTime.Now + timeSpan;
    }
}