using System;
using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.DateTimeExtensions {
    public static partial class DateTimeExtensions {
        public static IEnumerable<DateTime> Dates() 
            => Dates(DateTime.Today.Year);

        public static IEnumerable<DateTime> Dates(this int year) 
            => Enumerable.Range(1, 12).SelectMany(month => Dates(year, month)).ToList();

        public static IEnumerable<DateTime> Dates(int year, int month) 
            => Enumerable.Range(1, DateTime.DaysInMonth(year, month))
                .Select(day => new DateTime(year, month, day));
    }
}