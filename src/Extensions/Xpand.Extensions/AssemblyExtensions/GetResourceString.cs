using System.Reflection;
using Xpand.Extensions.StreamExtensions;

namespace Xpand.Extensions.AssemblyExtensions {
    public static partial class AssemblyExtensions {
        public static string GetResourceString(this Assembly assembly, string name) 
            => assembly.GetManifestResourceStream(name).ReadToEndAsString();
    }
}