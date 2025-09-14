using System.Collections.Concurrent;
using System.Threading;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub{
    public class AsyncLocalFactory {
        public static readonly ConcurrentBag<IAsyncLocal> RegisteredContexts = new();
        public static AsyncLocal<T> NewContext<T>() {
            var asyncLocal = new AsyncLocal<T>();
            RegisteredContexts.Add(asyncLocal.Wrap());
            return asyncLocal;
        }
    }
}