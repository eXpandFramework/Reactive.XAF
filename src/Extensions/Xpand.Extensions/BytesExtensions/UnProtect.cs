using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.BytesExtensions {
    public static partial class BytesExtensions {
        public static string UnProtect(this DataProtectionScope? scope,byte[] bytes) {
            if (scope!=null) {
                try { return bytes.UnProtect(scope.Value).First().GetString();}
                catch (CryptographicException) { }
            }
            return bytes.GetString();
        }

        public static SecureString[] UnProtect(this byte[] bytes,
            DataProtectionScope scope = DataProtectionScope.LocalMachine) {
            var strings = new List<SecureString>();
            var unprotectedBytes = ProtectedData.Unprotect(bytes, null,scope);
            using var memory = new MemoryStream(unprotectedBytes);
            using var reader = new BinaryReader(memory, Encoding.UTF8);

            while (memory.Position != memory.Length) {
                var current = new SecureString();
                strings.Add(current);
                var len = reader.ReadInt32();
                while (len-- > 0) {
                    current.AppendChar(reader.ReadChar());
                }
            }

            Array.Clear(bytes, 0, bytes.Length);
            Array.Clear(unprotectedBytes, 0, unprotectedBytes.Length);
            return strings.ToArray();
        }
    }
}