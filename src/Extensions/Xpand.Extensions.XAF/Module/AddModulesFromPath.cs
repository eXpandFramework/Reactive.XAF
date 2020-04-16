using System.IO;
using System.Linq;
using DevExpress.ExpressApp;
using Xpand.Extensions.AppDomain;

namespace Xpand.Extensions.XAF.Module{
    public static partial class ModulebaseExtensions{
        public static void AddModulesFromPath(this ModuleBase module,string pattern){
            var moduleTypes = Directory.GetFiles(System.AppDomain.CurrentDomain.ApplicationPath(), pattern)
                .Select(System.Reflection.Assembly.LoadFile)
                .SelectMany(assembly => assembly.GetTypes()).Where(type =>!type.IsAbstract&& typeof(ModuleBase).IsAssignableFrom(type));
            module.RequiredModuleTypes.AddRange(moduleTypes);
        }

    }

}