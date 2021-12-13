using System;
using System.Text;

namespace Xpand.Extensions.BytesExtensions {
    public static partial class BytesExtensions {
        public static string BitConvert(this Byte[] bytes, Encoding encoding = null) => BitConverter.ToString(bytes);
    }
}