using System;

namespace Xpand.Extensions.Numeric {
    public static partial class NumericExtensions {
        public static decimal PercentageDifference(this decimal d, decimal d2,int decimals=2) 
            => d>0? ((d - d2) / d.Abs() * 100).RoundDown(decimals):0;
        public static decimal Percentage(this decimal d, decimal percentage,int decimals=2) 
            => (percentage / 100 * d).RoundDown(decimals);
        public static decimal Percentage(this decimal d, int percentage,int decimals=2) 
            => d.Percentage(Convert.ToDecimal(percentage),decimals);
    }
}