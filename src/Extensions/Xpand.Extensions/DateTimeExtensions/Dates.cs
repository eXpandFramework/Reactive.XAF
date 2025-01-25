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
        
        public static DateTime ToOppositeDirection(this DateTime date) { 
            if(date <= DateTime.Now) return date;
            if (date==DateTime.MaxValue)return DateTime.MinValue;
            var diff = date - DateTime.Now;
            return DateTime.Now - diff;
        }
    }
}