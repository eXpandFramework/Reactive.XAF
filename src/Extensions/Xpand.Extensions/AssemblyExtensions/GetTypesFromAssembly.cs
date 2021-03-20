using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Xpand.Extensions.AssemblyExtensions {
    public static partial class AssemblyExtensions {
        public static IEnumerable<Type> GetTypesFromAssembly(this Assembly assembly) {
            try {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e) {
                return e.Types.Where(type => type != null && type.IsVisible);
            }
        }
    }
}