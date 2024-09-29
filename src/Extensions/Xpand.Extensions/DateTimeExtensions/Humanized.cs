using System;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.DateTimeExtensions {
    public static partial class DateTimeExtensions {
        public static string Humanized(this TimeSpan timeSpan)
            => timeSpan switch {
                _ when timeSpan.TotalDays >= 1
                    => new[] { ((int)timeSpan.TotalDays).ToString(), timeSpan.Hours.ToString(), "h ", timeSpan.Minutes.ToString(), "m ", timeSpan.Seconds.ToString(), "s" }.JoinString(),
                _ when timeSpan.TotalHours >= 1
                    => new[] { ((int)timeSpan.TotalHours).ToString(), "h ", timeSpan.Minutes.ToString(), "m ", timeSpan.Seconds.ToString(), "s" }.JoinString(),
                _ when timeSpan.TotalMinutes >= 1
                    => new[] { ((int)timeSpan.TotalMinutes).ToString(), "m ", timeSpan.Seconds.ToString(), "s" }.JoinString(),
                _ => new[] { ((int)timeSpan.TotalSeconds).ToString(), "s" }.JoinString()
            };
    }
}