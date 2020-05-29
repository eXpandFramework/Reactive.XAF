using System.Linq;
using System.Reflection;

namespace Xpand.Extensions.ReflectionExtensions{
    public partial class ReflectionExtensions{
        public static bool HasSameSignature(this MethodInfo a, MethodInfo b){
            bool sameParams = a.GetParameters().All(x => b.GetParameters().Any(y => x == y));
            bool sameReturnType = a.ReturnType == b.ReturnType;
            return sameParams && sameReturnType;
        }
    }
}