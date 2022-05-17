namespace Xpand.Extensions.Numeric {
    public static partial class NumericExtensions {
        public static decimal PercentageDifference(this decimal d, decimal d2,int decimals=2) 
            => d>0? ((d - d2) / d.Abs() * 100).RoundNumber(decimals):0;
    }
}