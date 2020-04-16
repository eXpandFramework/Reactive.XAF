using System.Linq;
using JetBrains.Annotations;

namespace Xpand.Extensions.Assembly{
    public static partial class AssemblyExtensions{
        [PublicAPI]
        public static string PublicKeyToken(this System.Reflection.AssemblyName assemblyName){
            return string.Join("", assemblyName.GetPublicKeyToken().Select(b => b.ToString("x2")));
        }
    }
}