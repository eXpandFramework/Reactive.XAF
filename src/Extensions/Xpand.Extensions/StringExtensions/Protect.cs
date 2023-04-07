using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
        public static byte[] Protect(this byte[] bytes, DataProtectionScope scope = DataProtectionScope.LocalMachine) 
            => ProtectedData.Protect(bytes, null,scope);

        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
        public static byte[] Protect(this string s, DataProtectionScope scope = DataProtectionScope.LocalMachine) 
            => s.Bytes().Protect(scope);
    }
}