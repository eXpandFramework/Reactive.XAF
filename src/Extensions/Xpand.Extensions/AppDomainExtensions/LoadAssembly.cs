using System;
using System.IO;
using System.Reflection;
using Xpand.Extensions.StreamExtensions;

namespace Xpand.Extensions.AppDomainExtensions{
    public static partial class AppDomainExtensions{
        public static Assembly LoadAssembly(this AppDomain appDomain, string assemblyPath) 
	        => Assembly.LoadFrom(Path.GetFullPath(assemblyPath));

        public static Assembly LoadAssembly(this AppDomain appDomain, Stream stream) 
	        => Assembly.Load(stream.Bytes());
        
        public static Assembly LoadAssembly(this AppDomain appDomain, byte[] bytes) 
	        => Assembly.Load(bytes);
    }
}