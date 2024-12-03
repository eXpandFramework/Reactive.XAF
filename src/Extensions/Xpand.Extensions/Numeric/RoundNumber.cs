using System;

namespace Xpand.Extensions.Numeric {
    public static partial class NumericExtensions {
        public static decimal SmartRound(this double d)
            => Convert.ToDecimal(d).SmartRound();
        
        public static decimal SmartRound(this decimal d) 
            => d.RoundNumber((d.FirstNonZeroDecimalIndex()+2));

        public static decimal RoundNumber(this decimal d, int decimals = 0) 
            => Math.Round(d, decimals);
        
        public static decimal RoundDown(this decimal number, int decimalPlaces) => Math.Floor(number * (decimal)Math.Pow(10, decimalPlaces)) / (decimal)Math.Pow(10, decimalPlaces);

        public static decimal RoundUp(this decimal number, int decimalPlaces) => Math.Ceiling(number * (decimal)Math.Pow(10, decimalPlaces)) / (decimal)Math.Pow(10, decimalPlaces);
    }
}