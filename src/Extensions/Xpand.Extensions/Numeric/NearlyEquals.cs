using System;

namespace Xpand.Extensions.Numeric {
    public static partial class NumericExtensions {
        public static bool NearlyEquals(this double value1, double value2, double unimportantDifference = 0.0001) 
            => Math.Abs(value1 - value2) < unimportantDifference;
    }
}