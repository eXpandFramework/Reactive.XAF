namespace Xpand.Extensions.Numeric {
    public static partial class NumericExtensions {
        public static bool GreaterOrEqual(this double value1, double value2, double unimportantDifference = 0.0001) 
            => value1.NearlyEquals(value2) || value1 > value2;
    }
}