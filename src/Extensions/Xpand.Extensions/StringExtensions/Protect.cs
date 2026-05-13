using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
using Xpand.Extensions.BytesExtensions;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
        public static byte[] Protect(this byte[] bytes, DataProtectionScope scope = DataProtectionScope.LocalMachine) 
            => ProtectedData.Protect(bytes, null,scope);

        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
        public static byte[] Protect(this string s, DataProtectionScope scope = DataProtectionScope.LocalMachine) 
            => s.Bytes().Protect(scope);
        
        public static void SaveSecret(this Environment.SpecialFolder applicationData,string name,string value,string directoryName="secrets"){
            var folderPath = Environment.GetFolderPath(applicationData);
            var secretsDir = $"{folderPath}\\{directoryName}";
            if (!Directory.Exists(secretsDir)){
                Directory.CreateDirectory(secretsDir);
            }
            value.Bytes().Protect().Save($"{secretsDir}\\{name}");
        }

    }
}