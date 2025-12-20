using System;

namespace Xpand.Extensions.DateTimeExtensions {
    public static partial class DateTimeExtensions {
        public static TimeSpan TimeToPast(this DateTime dateTime)
            => dateTime > DateTime.Now ? dateTime.Subtract(DateTime.Now) : System.TimeSpan.Zero;
    }
}