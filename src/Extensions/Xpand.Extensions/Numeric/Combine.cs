namespace Xpand.Extensions.Numeric {
    public static partial class NumericExtensions {
        public static int Combine(this long a, long b) 
            => (int)(a >> 32) ^ (int)a ^ (int)(b >> 32) ^ (int)b;
    }
}