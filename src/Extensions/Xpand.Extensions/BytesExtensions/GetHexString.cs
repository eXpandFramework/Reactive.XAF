using Cysharp.Text;

namespace Xpand.Extensions.BytesExtensions {
    public static partial class BytesExtensions {
         
        public static string GetHexString(this byte[] buff) {
            using var sb = ZString.CreateUtf8StringBuilder();
            foreach (var b in buff) sb.AppendFormat("{0:X2}", b);
            return sb.ToString();
        }
    }
}