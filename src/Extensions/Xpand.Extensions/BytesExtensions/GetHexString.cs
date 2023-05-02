using System.Linq;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.BytesExtensions {
    public static partial class BytesExtensions {
        public static string GetHexString(this byte[] buff) 
            => buff.Select(b => b.ToString("X2")).Join("");
    }
}