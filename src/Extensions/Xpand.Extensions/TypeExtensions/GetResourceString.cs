using System;
using Xpand.Extensions.StreamExtensions;

namespace Xpand.Extensions.TypeExtensions {
    public static partial class TypeExtensions {
        public static string GetResourceString(this Type type, string name) 
            => type.Assembly.GetManifestResourceStream(type, name).ReadToEndAsString();
    }
}