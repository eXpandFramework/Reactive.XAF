using System;

namespace Xpand.Extensions.Numeric {
    public static partial class NumericExtensions {
        public static double Round(this double d, int decimals = 0) 
            => Math.Round(d, decimals);
    }
}