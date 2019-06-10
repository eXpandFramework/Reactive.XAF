using System;
using System.Linq;
using System.Reflection;
using Fasterflect;

namespace Xpand.Source.Extensions.System.AppDomain{
    internal static partial class AppDomainExtensions{
        public static Assembly AssemblySystemWeb(this global::System.AppDomain appDomain){
            return appDomain.GetAssemblies().FirstOrDefault(_ => _.GetName().Name == "System.Web");
        }
        public static Type TypeUnit(this Assembly assembly){
            return assembly.GetType("System.Web.UI.WebControls.Unit");
        }
        public static MethodInvoker Percentage(this Type type){
            return type.GetMethods(BindingFlags.Public|BindingFlags.Instance|BindingFlags.Static).First(info => info.Name=="Percentage").DelegateForCallMethod();
        }

    }
}