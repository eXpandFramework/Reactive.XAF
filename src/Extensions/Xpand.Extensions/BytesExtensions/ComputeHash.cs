using System;
using System.Security.Cryptography;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.BytesExtensions {
    public static partial class BytesExtensions {
        public static byte[] ComputeHash(this byte[] secret, string message, HashAlgorithm algorithm = null) {
            algorithm ??= new HMACSHA256(secret);
            return algorithm.ComputeHash(message.Bytes());
        }
    }
}