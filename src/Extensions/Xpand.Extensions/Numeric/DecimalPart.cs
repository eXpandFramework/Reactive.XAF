using System;
using System.Globalization;
using System.Linq;

namespace Xpand.Extensions.Numeric {
    public static partial class NumericExtensions {
        public static decimal DecimalPart(this decimal d) {
            var decimalPart = d - Math.Truncate(d);
            var length = decimalPart.ToString(CultureInfo.InvariantCulture).Length-2;
            return decimalPart.RoundToSignificantDigits(length);
        }

        public static int DecimalPartLength(this decimal d)
            => d.ToString(CultureInfo.InvariantCulture)
                .Split('.').Skip(1).SelectMany(s => s.TrimEnd('0')).Count();
    }
}