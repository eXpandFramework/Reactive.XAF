using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.DateTimeExtensions {
    public static partial class DateTimeExtensions {
        public static DateTime? Parse(this DateTimeStyles dateTimeStyles, string text) {
            text = $"{text}".Trim();
            if (new[] { "TBA", "-", "TBD" }.Any(s => text.Contains(s, StringComparison.OrdinalIgnoreCase)) || text == "") return null;
            text = text.RegexReplace(@"^(?:Mon(?:day)?|Tue(?:sday)?|Wed(?:nesday)?|Thu(?:rsday)?|Fri(?:day)?|Sat(?:urday)?|Sun(?:day)?),\s*", string.Empty, RegexOptions.IgnoreCase);
            text = text.RegexReplace(@"(\d+)(st|nd|rd|th)", "$1")
                .Remove("(UTC)", "UTC", "at ", " of", ",")
                .RegexReplace(@"(\d{1,2})\.(\d{2})(?=\s|$)", "$1:$2")
                .RegexReplace(@"(?<=\b(?:1[3-9]|2[0-3]):\d{2})\s*(?:am|pm)\b", string.Empty, RegexOptions.IgnoreCase)
                .RegexReplace(@"(\d{1,2}:\d{2})(?:\s?(am|pm))\s*$",
                    m => $"{m.Groups[1].Value} {m.Groups[2].Value.ToUpperInvariant()}", RegexOptions.IgnoreCase)
                .RegexReplace(@"\s+", " ")
                .Trim();
            if (!text.RegexIsMatch(@"\d{4}"))
                text = $"{text} {DateTime.UtcNow.Year}";


            var dateTime = DateTimeOffset.ParseExact(text, [
                "M/d/yyyy h:mm tt",
                "MM/dd/yyyy HH:mm:ss",
                "MM/dd/yyyy HH:mm",
                "d MMM HH:mm yyyy",
                "MMM d yyyy hh:mm tt",
                "d MMM yyyy, HH:mm",
                "d MMMM yyyy, HH:mm",
                "d MMM, HH:mm",
                "d MMM HH:mm",
                "d MMMM HH:mm",
                "d MMMM, HH:mm",
                "d MMM yyyy HH:mm",
                "d MMMM yyyy HH:mm",
                "dd/MM/yyyy HH:mm",
                "HH:mm MMM dd yyyy",
                "HH:mm MMMM dd yyyy",
                "d MMM yyyy h:mm tt",
                "d MMM yyyy hh:mm tt",
                "d MMMM yyyy h:mm tt",
                "d MMMM yyyy hh:mm tt",
                "MMMM d yyyy, HH:mm",
                "MMMM d yyyy hh:mm tt",
                "MMMM d yyyy HH:mm",
                "MMMM d, HH:mm",
                "MMM d yyyy, HH:mm",
                "MMM d yyyy HH:mm",
                "MMM d, HH:mm",
                "MMM d, yyyy hh:mm tt",
                "MMMM d, yyyy hh:mm tt",
                "d MMM yyyy",
                "d MMMM yyyy",
                "d MMMM HH:mm yyyy",
                "MMMM d yyyy",
                "MMMM d",
                "MMM d",
                "d MMMM",
                "HH:mm d MMM yyyy",
                "hh:mm tt MMM d yyyy",
                "hh:mm tt MMM d, yyyy",
                "hh:mm tt d MMM yyyy",
                "d MMMM h:mm tt yyyy",      
            ], CultureInfo.InvariantCulture).DateTime;

            return dateTimeStyles == DateTimeStyles.AssumeUniversal ? dateTime.ToLocalTime() : dateTime;
        }
    }
}