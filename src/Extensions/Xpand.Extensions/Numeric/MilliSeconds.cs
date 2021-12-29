using System;

namespace Xpand.Extensions.Numeric {
    public static partial class NumericExtensions {
        public static long MilliSeconds(this int milliSeconds) => TimeSpan.FromMilliseconds(milliSeconds).Ticks;
    }
}