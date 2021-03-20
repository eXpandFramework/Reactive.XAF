using System;
using Xpand.Extensions.StreamExtensions;

namespace Xpand.Extensions.TypeExtensions {
    public static partial class TypeExtensions {
        public static void WriteResourceToFile(this Type type, string resourceName, string filePath)
            => type.Assembly.GetManifestResourceStream(type, resourceName).SaveToFile(filePath);

    }
}