using System.IO;
using System.Text;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static MemoryStream MemoryStream(this string s, Encoding encoding = null) => new(s.Bytes(encoding));
    }
}