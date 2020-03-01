using System.IO;

namespace Xpand.Extensions.AppDomain{
    public static partial class AppDomainExtensions{
        public static System.Reflection.Assembly LoadAssembly(this global::System.AppDomain appDomain,
            string assemblyPath){
            return System.Reflection.Assembly.LoadFile(Path.GetFullPath(assemblyPath));
        }
    }
}