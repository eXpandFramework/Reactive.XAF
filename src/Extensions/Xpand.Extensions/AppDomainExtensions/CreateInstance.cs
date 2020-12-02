using System;
using Fasterflect;

namespace Xpand.Extensions.AppDomainExtensions {
    public static partial class AppDomainExtensions {
        public static object CreateTypeInstance(this AppDomain domain, string fullName, params object[] parameters)
            => domain.GetAssemblyType(fullName).CreateInstance(parameters);
    }
}