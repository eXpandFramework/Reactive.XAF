using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.BytesExtensions {
    public static partial class BytesExtensions {
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
        public static byte[] UnProtect(this DataProtectionScope? scope,byte[] bytes) {
            if (scope!=null) {
                try { return bytes.UnProtect(scope.Value);}
                catch (CryptographicException) { }
            }
            return bytes;
        }
        
        public static byte[] ToByteArray(this SecureString secureString) 
            => new NetworkCredential("", secureString).Password.Bytes();

        public static T Process<T>(this SecureString secureString, Func<byte[], T> func) {
            var bstr = IntPtr.Zero;
            byte[] workArray = null;
            GCHandle? handle = null; 
            try {
                bstr = Marshal.SecureStringToBSTR(secureString);
                unsafe {
                    var bstrBytes = (byte*)bstr;
                    workArray = new byte[secureString.Length * 2];
                    handle = GCHandle.Alloc(workArray, GCHandleType.Pinned);
                    for (var i = 0; i < workArray.Length; i++)
                        workArray[i] = *bstrBytes++;
                }
                return func(workArray);
            }
            finally {
                if (workArray != null)
                    for (var i = 0; i < workArray.Length; i++)
                        workArray[i] = 0;
                handle?.Free();
                if (bstr != IntPtr.Zero)
                    Marshal.ZeroFreeBSTR(bstr);
            }
        }


        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
        public static SecureString[] UnProtectSecuredString(this byte[] bytes,
            DataProtectionScope scope = DataProtectionScope.LocalMachine) {
            var strings = new List<SecureString>();
            var unprotectedBytes = UnProtect(bytes, scope);
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

        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
        public static byte[] UnProtect(this byte[] bytes, DataProtectionScope scope=DataProtectionScope.LocalMachine) 
            => ProtectedData.Unprotect(bytes, null, scope);
    }
    
      
}