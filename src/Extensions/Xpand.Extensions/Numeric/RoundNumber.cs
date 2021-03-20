using System;

namespace Xpand.Extensions.Numeric {
    public static partial class NumericExtensions {
        public static decimal RoundNumber(this decimal d, int decimals = 0) 
            => Math.Round(d, decimals);
    }
}