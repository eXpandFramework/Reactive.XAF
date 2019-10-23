using System.IO;
using System.Linq;
using System.Reflection;
using DevExpress.ExpressApp;
using Xpand.Extensions.AppDomain;

namespace Xpand.Extensions.XAF.Module{
    public static class ModulebaseExtensions{
        public static void AddModulesFromPath(this ModuleBase module,string pattern){
            var moduleTypes = Directory.GetFiles(System.AppDomain.CurrentDomain.ApplicationPath(), pattern)
                .Select(Assembly.LoadFile)
                .SelectMany(assembly => assembly.GetTypes()).Where(type =>!type.IsAbstract&& typeof(ModuleBase).IsAssignableFrom(type));
            module.RequiredModuleTypes.AddRange(moduleTypes);
        }

    }
}