using System;
using System.CodeDom.Compiler;
using System.Linq;

namespace Xpand.Extensions.Compiler{
    public static class CompilerExtensions{
        public static void ReferenceNetStandard(this CompilerParameters parameters){
            var netStandard = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetName().Name.Contains("netstandard"))?.Location;
            if (netStandard != null){
                parameters.ReferencedAssemblies.Add(netStandard);
            }
        }
    }
}