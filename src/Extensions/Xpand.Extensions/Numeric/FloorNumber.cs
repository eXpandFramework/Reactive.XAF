using System;

namespace Xpand.Extensions.Numeric {
    public static partial class NumericExtensions {
        public static decimal FloorNumber(this decimal d, int decimals = 0)
            => Math.Floor(d);
    }
}