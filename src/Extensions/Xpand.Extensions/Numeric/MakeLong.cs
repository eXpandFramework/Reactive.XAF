namespace Xpand.Extensions.Numeric {
    public static partial class NumericExtensions {
        public static long MakeLong(this int left, int right) {
            long res = left;
            res = (res << 32);
            res |= (uint)right;
            return res;
        }
    }
}