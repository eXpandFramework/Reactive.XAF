using System;

namespace Xpand.Extensions.Numeric {
    public static partial class NumericExtensions {
        public static double RoundToSignificantDigits(this double d, int digits) {
            if (d.NearlyEquals(0)) return d;
            var scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) + 1);
            return scale * Math.Round(d / scale, digits);
        }
    }
}