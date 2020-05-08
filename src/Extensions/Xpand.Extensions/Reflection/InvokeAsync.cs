using System.Reflection;
using System.Threading.Tasks;
using Fasterflect;

namespace Xpand.Extensions.Reflection{
    public partial class ReflectionExtensions{
        public static async Task<object> InvokeAsync(this MethodInfo methodInfo, object obj, params object[] parameters){
            dynamic awaitable = methodInfo.Invoke(obj, parameters);
            await (System.Threading.Tasks.Task)awaitable;
            return awaitable.GetAwaiter().GetResult();
        }
    }
}