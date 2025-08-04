using System;
using System.Threading.Tasks;

namespace Xpand.Extensions.ObjectExtensions {
    public static partial class ObjectExtensions {
        public static Task AwaitAsync<T>(this T any, Action<T> invoker) 
            => Task.Run(() => invoker(any));
    }
}