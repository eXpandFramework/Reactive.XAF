using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.XAF.ModuleExtensions {
    public static partial class ModuleBaseExtensions {
        public static IEnumerable<ModuleBase> RequiredModules(this ModuleBase moduleBase) 
            => moduleBase.RequiredModuleTypes.Select(type
                    => moduleBase.Application.Modules.First(@base => @base.GetType() == type))
                .SelectManyRecursive(@base => @base.RequiredModuleTypes.Select(type
                    => moduleBase.Application.Modules.First(moduleBase1 => moduleBase1.GetType() == type)))
                .Distinct();
    }
}