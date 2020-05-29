using System.Linq;
using Fasterflect;

namespace Xpand.Extensions.AppDomainExtensions{
    public static partial class AppDomainExtensions{
        public static System.Reflection.Assembly AssemblyDevExpressExpressAppWeb(this System.AppDomain appDomain) => appDomain
            .GetAssemblies().FirstOrDefault(_ => _.GetName().Name.StartsWith("DevExpress.ExpressApp.Web.v"));

        public static System.Type TypeClientSideEventsHelper(this System.Reflection.Assembly assembly) => assembly
            .GetType("DevExpress.ExpressApp.Web.Utils.ClientSideEventsHelper");

        public static MethodInvoker AsssignClientHanderSafe(this System.Type type) => type
            .Methods(Flags.Static|Flags.Instance|Flags.Public,"AssignClientHandlerSafe").First(info => info.Parameters().Count==4).DelegateForCallMethod();
    }
}