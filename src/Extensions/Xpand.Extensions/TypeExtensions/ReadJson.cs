using System;
using System.IO;
using System.Threading.Tasks;
using Xpand.Extensions.AssemblyExtensions;
using Xpand.Extensions.StreamExtensions;

namespace Xpand.Extensions.TypeExtensions {
    public static partial class EnumExtensions {
        public static async Task<string> ReadJson(this Type type, string resourceName)
            => await type.Assembly.GetManifestResourceStream(name =>
                    Path.GetFileNameWithoutExtension(name) == $"{type.Namespace}.{resourceName}")
                .ReadToEndAsStringAsync();
    }
}