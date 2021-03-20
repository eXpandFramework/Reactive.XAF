using System;

namespace Xpand.Extensions.DateTimeExtensions {
    public static partial class DateTimeExtensions {
        public static int Months(this TimeSpan timespan)
            => (int) (timespan.Days / 30.436875);
    }
}