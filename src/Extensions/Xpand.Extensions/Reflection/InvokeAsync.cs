using System.Reflection;
using System.Threading.Tasks;
using Fasterflect;

namespace Xpand.Extensions.Reflection{
    public partial class ReflectionExtensions{
        public static async Task<object> InvokeAsync(this MethodInfo @this, object obj, params object[] parameters){
            var awaitable = @this.Invoke(obj, parameters);
            await (System.Threading.Tasks.Task)awaitable;
            return awaitable.CallMethod("GetAwaiter").CallMethod("GetResult");
        }
    }
}