using System;

namespace Xpand.Extensions.Numeric {
    public static partial class NumericExtensions {
        public static decimal Abs(this decimal d) => Math.Abs(d);
        public static double Abs(this double d) => Math.Abs(d);
        public static int Abs(this int d) => Math.Abs(d);
    }
}