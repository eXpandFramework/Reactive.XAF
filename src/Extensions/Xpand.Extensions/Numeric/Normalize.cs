namespace Xpand.Extensions.Numeric {
    public static partial class NumericExtensions {
        public static decimal Normalize(this decimal value)
            => value / 1.000000000000000000000000000000000m;
    }
}