using System;
using System.Text;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.StringExtensions{
    public static partial class StringExtensions{
        public static byte[] Bytes(this string s,Encoding encoding=null) 
            => s==null?Array.Empty<byte>():(encoding??Encoding.UTF8).GetBytes(s);
    }
}