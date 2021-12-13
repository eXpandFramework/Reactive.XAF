using System.Security.Cryptography;
using System.Text;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.BytesExtensions {
    public static partial class BytesExtensions {
        public static string CryptoSign(this byte[] secret, string message,HashAlgorithm algorithm=null) {
            algorithm ??= new HMACSHA256(secret);
            return algorithm.ComputeHash(message.Bytes(Encoding.ASCII)).GetHexString();
            
        }
    }
}