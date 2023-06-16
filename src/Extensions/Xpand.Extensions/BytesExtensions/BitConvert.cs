using System;

namespace Xpand.Extensions.BytesExtensions {
    public static partial class BytesExtensions {
        
        public static string BitConvert(this Byte[] bytes) 
            => BitConverter.ToString(bytes);
    }
}