using System;
using System.Globalization;

namespace Xpand.Extensions.Numeric {
    public static partial class NumericExtensions {
        public static decimal DecimalPart(this decimal d) {
            var decimalPart = d - Math.Truncate(d);
            var length = decimalPart.ToString(CultureInfo.InvariantCulture).Length-2;
            return decimalPart.RoundToSignificantDigits(length);
        }

        public static int DecimalPartLenght(this decimal d)
            => d.DecimalPart().ToString(CultureInfo.InvariantCulture).Length-2;
    }
}