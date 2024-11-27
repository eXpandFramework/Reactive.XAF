using System;
using System.Globalization;
using System.Linq;

namespace Xpand.Extensions.Numeric {
    public static partial class NumericExtensions {
        public static int FirstNonZeroDecimalIndex(this decimal number) {
            number = Math.Abs(number - Math.Truncate(number));
            int index = 1;
            while (number > 0) {
                number *= 10;
                if ((int)Math.Truncate(number) != 0) return index;
                number -= Math.Truncate(number);
                index++;
            }
            return -1;
        }

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