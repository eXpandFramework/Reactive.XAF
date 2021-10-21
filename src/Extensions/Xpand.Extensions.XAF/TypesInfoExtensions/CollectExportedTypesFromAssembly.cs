using System;
using System.Collections.Generic;
using System.Reflection;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.TypesInfoExtensions {
    public static partial class TypesInfoExtensions {
        public static IEnumerable<Type> CollectExportedTypesFromAssembly(this Type type)
            => type.Assembly.CollectExportedTypesFromAssembly();
        
        public static IEnumerable<Type> CollectExportedTypesFromAssembly(this Assembly assembly)
            => ModuleHelper.CollectExportedTypesFromAssembly(assembly, type1 => type1.IsExportedType());
    }
}