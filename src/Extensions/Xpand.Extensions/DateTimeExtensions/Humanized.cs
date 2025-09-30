﻿using System;
using Humanizer;
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
        
        public static string Humanize(this Type type,object value) {
            if (value == null)
                return "";
        
            if (type == typeof(DateTime) || type == typeof(DateTime?))
                return ((DateTime)value).Humanize ();
        
            if (type.IsEnum)
                return value.ToString().Humanize();
        
            if (type == typeof(bool) || type == typeof(bool?))
                return ((bool)value) ? "Yes" : "No";
        
            if (type == typeof(decimal) || type == typeof(double) || type == typeof(float) ||
                type == typeof(int) || type == typeof(long) || type == typeof(short) ||
                type == typeof(byte) || type == typeof(uint) || type == typeof(ulong) ||
                type == typeof(ushort) || type == typeof(sbyte))
                return value.ToString();
        
            return type == typeof(string) ? ((string)value).Humanize() : value.ToString().Humanize();
        }
        
        public static string HumanizeCompact(this DateTime? dateTime) =>dateTime?.HumanizeCompact();
        public static string HumanizeCompact(this DateTime dateTime) =>dateTime==DateTime.MinValue?null: dateTime.Humanize(utcDate: false,DateTime.Now).Replace(" from now"," later");
    }
}