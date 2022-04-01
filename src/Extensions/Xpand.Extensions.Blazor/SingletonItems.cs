using System.Collections.Concurrent;

namespace Xpand.Extensions.Blazor {
    public class SingletonItems:ConcurrentDictionary<object,object>{
    }
}