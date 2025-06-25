using System;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.DateTimeExtensions {
    public static partial class DateTimeExtensions {
        public static string Humanized(this TimeSpan timeSpan, string format = null)
            => !string.IsNullOrEmpty(format)
                ? timeSpan.ToHourMinuteFormat(format)
                : timeSpan switch {
                    _ when timeSpan.TotalDays >= 1
                        => new[] { ((int)timeSpan.TotalDays).ToString(), timeSpan.Hours.ToString(), "h ", timeSpan.Minutes.ToString(), "m ", timeSpan.Seconds.ToString(), "s" }.JoinString(),
                    _ when timeSpan.TotalHours >= 1
                        => new[] { ((int)timeSpan.TotalHours).ToString(), "h ", timeSpan.Minutes.ToString(), "m ", timeSpan.Seconds.ToString(), "s" }.JoinString(),
                    _ when timeSpan.TotalMinutes >= 1
                        => new[] { ((int)timeSpan.TotalMinutes).ToString(), "m ", timeSpan.Seconds.ToString(), "s" }.JoinString(),
                    _ => new[] { ((int)timeSpan.TotalSeconds).ToString(), "s" }.JoinString()
                };

        static string ToHourMinuteFormat(this TimeSpan timeSpan, string format)
            => format switch {
                "HH:mm" => $"{((int)timeSpan.TotalHours % 24 + 24) % 24:00}:{timeSpan.Minutes:00}",
                "mm:ss" => $"{(int)timeSpan.TotalMinutes:00}:{timeSpan.Seconds:00}",
                _ => timeSpan.ToString(format)
            };
    }
}