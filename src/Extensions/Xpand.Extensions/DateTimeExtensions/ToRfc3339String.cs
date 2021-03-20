using System;
using System.Globalization;

namespace Xpand.Extensions.DateTimeExtensions{
    public static partial class DateTimeExtensions{
        public static string ToRfc3339String(this DateTime dateTime) 
            => dateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", DateTimeFormatInfo.InvariantInfo);
    }
}