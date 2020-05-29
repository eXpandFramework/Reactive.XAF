using System.Reflection;
using System.Threading.Tasks;

namespace Xpand.Extensions.ReflectionExtensions{
    public partial class ReflectionExtensions{
        public static async Task<object> InvokeAsync(this MethodInfo methodInfo, object obj, params object[] parameters){
            dynamic awaitable = methodInfo.Invoke(obj, parameters);
            await (Task)awaitable;
            return awaitable.GetAwaiter().GetResult();
        }
    }
}