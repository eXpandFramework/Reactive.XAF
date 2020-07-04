using System.Linq;
using System.Reflection;

namespace Xpand.Extensions.AppDomainExtensions{
    public static partial class AppDomainExtensions{
	    public static Assembly SystemWebAssembly(this IAppDomainWeb appDomain) => appDomain.AppDomain
            .GetAssemblies().FirstOrDefault(_ => _.GetName().Name == "System.Web");
    }

    
}