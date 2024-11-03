using System;
using System.Linq;
using Xpand.Extensions.BytesExtensions;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static string HexToUtf8(string hexMessage) {
            if (hexMessage.StartsWith("0x")) {
                hexMessage = hexMessage.Substring(2);
            }

            return Enumerable.Range(0, hexMessage.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hexMessage.Substring(x, 2), 16))
                .ToArray().GetString();
        }
    }
}