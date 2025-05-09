using System;

namespace Xpand.Extensions.DateTimeExtensions {
    public static partial class DateTimeExtensions {
        public static bool IsBetween(this DateTime dateTime, DateTime left, DateTime right)
            => dateTime > left && dateTime < right;
    }
}