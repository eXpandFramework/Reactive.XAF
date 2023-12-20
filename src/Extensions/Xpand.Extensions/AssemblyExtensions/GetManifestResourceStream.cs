using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Xpand.Extensions.StreamExtensions;

namespace Xpand.Extensions.AssemblyExtensions {
    public static partial class AssemblyExtensions {
        public static Stream GetManifestResourceStream(this Assembly assembly, Func<string, bool> nameMatch)
            => assembly.GetManifestResourceStream(assembly.GetManifestResourceNames().First(nameMatch));
        public static byte[] GetManifestResourceBytes(this Assembly assembly, Func<string, bool> nameMatch) {
            using var stream = assembly.GetManifestResourceStream(assembly.GetManifestResourceNames().First(nameMatch));
            return stream.Bytes();
        }
    }
}