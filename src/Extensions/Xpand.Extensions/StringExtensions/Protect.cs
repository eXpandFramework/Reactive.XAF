using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

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