using System;

namespace Xpand.Extensions.Numeric {
    public static partial class NumericExtensions {
        public static long Seconds(this int seconds) => TimeSpan.FromSeconds(seconds).Ticks;
    }
}