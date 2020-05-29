using System.Linq;
using System.Reflection;
using Fasterflect;

namespace Xpand.Extensions.AppDomainExtensions{
    public static partial class AppDomainExtensions{
        public static Assembly AssemblySystemWeb(this System.AppDomain appDomain) => appDomain
            .GetAssemblies().FirstOrDefault(_ => _.GetName().Name == "System.Web");

        public static System.Type TypeUnit(this Assembly assembly) => assembly.GetType("System.Web.UI.WebControls.Unit");

        public static MethodInvoker Percentage(this System.Type type) => type
            .GetMethods(BindingFlags.Public|BindingFlags.Instance|BindingFlags.Static).First(info => info.Name=="Percentage").DelegateForCallMethod();
    }
}