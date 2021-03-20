using System;

namespace Xpand.Extensions.DateTimeExtensions {
    public static partial class DateTimeExtensions {
        public static int Years(this TimeSpan timespan) 
            => (int) (timespan.Days / 365.2425);
    }
}