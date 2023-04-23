using System;
using System.Text;

namespace Xpand.Extensions.StringExtensions{
    public static partial class StringExtensions{
        public static byte[] Bytes(this string s, Encoding encoding = null) {
            encoding ??= Encoding.UTF8;
            return s == null ? Array.Empty<byte>() : encoding.GetBytes(s);
        }
        
    }
}