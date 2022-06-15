using System.Linq;


namespace Xpand.Extensions.AssemblyExtensions{
    public static partial class AssemblyExtensions{
        
        public static string PublicKeyToken(this System.Reflection.AssemblyName assemblyName) => string.Join("", assemblyName.GetPublicKeyToken().Select(b => b.ToString("x2")));
    }
}