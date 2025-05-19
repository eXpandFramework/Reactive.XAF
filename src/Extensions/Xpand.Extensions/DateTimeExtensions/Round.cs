using System;

namespace Xpand.Extensions.DateTimeExtensions {
    public static partial class DateTimeExtensions {
        public enum RoundMode{
            Floor,
            Ceiling,
            Nearest,
            Force59
        }
        public static DateTime RoundToMinute(this DateTime dateTime, RoundMode mode=RoundMode.Nearest) 
            => mode switch {
                RoundMode.Floor => new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour,
                    dateTime.Minute, 0, dateTime.Kind),
                RoundMode.Ceiling => (dateTime.Second > 0 || dateTime.Millisecond > 0)
                    ? new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0,
                        dateTime.Kind).AddMinutes(1)
                    : new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0,
                        dateTime.Kind),
                RoundMode.Nearest => dateTime.Second >= 30
                    ? new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0,
                        dateTime.Kind).AddMinutes(1)
                    : new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0,
                        dateTime.Kind),
                RoundMode.Force59 => new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour,
                    dateTime.Minute, 59, dateTime.Kind),
                _ => dateTime
            };
    }
}