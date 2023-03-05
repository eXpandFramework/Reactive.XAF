using System;

namespace Xpand.Extensions.Numeric {
    public static partial class NumericExtensions {
        public static long Ticks(this int milliSeconds) => milliSeconds.Milliseconds().Ticks;
        public static TimeSpan Milliseconds(this int milliSeconds) => TimeSpan.FromMilliseconds(milliSeconds);
    }
}