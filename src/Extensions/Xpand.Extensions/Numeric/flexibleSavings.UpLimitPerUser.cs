using System;

namespace Xpand.Extensions.Numeric {
    public static partial class NumericExtensions {
        public static decimal ZeroWhenNative(this decimal d)
            => Math.Max(0, d);
    }
}