using System;
using System.Linq;
using Fasterflect;

namespace Xpand.Extensions.Fasterflect {
    public static class FasterflectExtensions {
        public static T CreateInstance<T>(this Type type,params object[] parameters) {
            if (type.IsArray&&!parameters.Any()) {
                parameters = new object[]{0};
            }
            return (T)type.CreateInstance(parameters);
        }
    }
}