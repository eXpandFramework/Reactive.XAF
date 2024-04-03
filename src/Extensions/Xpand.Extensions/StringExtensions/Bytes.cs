using System;
using System.Security.Cryptography;
using System.Text;

namespace Xpand.Extensions.StringExtensions{
    public static partial class StringExtensions{
        public static byte[] Combine(this string s, string other) {
            using (var sha256 = SHA256.Create()) {
                var aBytes = Encoding.UTF8.GetBytes(s);
                var bBytes = Encoding.UTF8.GetBytes(other);
                var aBase64Length = (aBytes.Length + 2) / 3 * 4;
                var bBase64Length = (bBytes.Length + 2) / 3 * 4;

                char[] delimiterChars = ['|'];
                var combinedLength = aBase64Length + delimiterChars.Length + bBase64Length;
                var combinedChars = new char[combinedLength];

                Span<char> combinedSpan = combinedChars;
                if (!Convert.TryToBase64Chars(aBytes, combinedSpan, out var aEncodedChars))
                    throw new InvalidOperationException("Failed to encode the first string to Base64.");
                combinedSpan = combinedSpan.Slice(aEncodedChars);
                delimiterChars.CopyTo(combinedSpan);
                combinedSpan = combinedSpan.Slice(delimiterChars.Length);
                if (!Convert.TryToBase64Chars(bBytes, combinedSpan, out _))
                    throw new InvalidOperationException("Failed to encode the second string to Base64.");

                var combinedBytes = Encoding.UTF8.GetBytes(combinedChars);
                var hashBytes = sha256.ComputeHash(combinedBytes);
                return hashBytes;
            }
        }

        public static byte[] Bytes(this string s, Encoding encoding = null) 
            => s == null ? Array.Empty<byte>() : (encoding ?? Encoding.UTF8).GetBytes(s);
    }
}