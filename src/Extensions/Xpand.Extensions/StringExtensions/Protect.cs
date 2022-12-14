using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
        public static byte[] Protect(this string s, DataProtectionScope scope = DataProtectionScope.LocalMachine) {
            using (var memory = new MemoryStream()) {
                using (var writer = new BinaryWriter(memory, Encoding.UTF8)) {
                    var chars = s.ToArray();
                    writer.Write(chars.Length);
                    foreach (var c in chars) writer.Write(c);
                    writer.Flush();
                    return ProtectedData.Protect(memory.ToArray(), null,scope);
                }    
            }
        }

    }
}