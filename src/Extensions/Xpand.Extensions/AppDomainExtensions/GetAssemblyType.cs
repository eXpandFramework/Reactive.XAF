using System;
using System.Linq;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.AppDomainExtensions{
    public static partial class AppDomainExtensions {
        public static Type GetAssemblyType(this AppDomain domain, string fullName,bool ignoreCase) 
            => fullName==null?null:domain.GetAssemblies().Select(assembly => assembly.GetType(fullName,ignoreCase)).WhereNotDefault().FirstOrDefault();
    }
}