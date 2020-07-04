using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Fasterflect;

namespace Xpand.Extensions.AppDomainExtensions{
    public static partial class AppDomainExtensions{
        public static Assembly LoadAssembly(this AppDomain appDomain, string assemblyPath) => Assembly
            .LoadFile(Path.GetFullPath(assemblyPath));
    }
}