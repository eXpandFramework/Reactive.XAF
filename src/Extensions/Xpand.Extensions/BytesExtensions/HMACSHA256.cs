using System.Security.Cryptography;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.BytesExtensions {
    public static partial class BytesExtensions {
        public static string CryptoSign(this byte[] secret, string message,HashAlgorithm algorithm=null) => secret.ComputeHash(message,algorithm).GetHexString();
    }
}