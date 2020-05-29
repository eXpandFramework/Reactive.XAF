using System.IO;

namespace Xpand.Extensions.AppDomainExtensions{
    public static partial class AppDomainExtensions{
        public static System.Reflection.Assembly LoadAssembly(this System.AppDomain appDomain, string assemblyPath) => System.Reflection.Assembly
            .LoadFile(Path.GetFullPath(assemblyPath));
    }
}