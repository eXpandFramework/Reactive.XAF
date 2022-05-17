using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Xpand.Extensions.Tracing{
    public interface IPush{
        void Push(string message,string source);
    }

    public static class TraceSourceExtensions {
        [SuppressMessage("ReSharper", "ForCanBeConvertedToForeach")]
        public static void Push(this TraceSource source,string message) {
            var listeneers = source.Listeners.OfType<IPush>().ToArray();
            for (var index = 0; index < listeneers.Length; index++) {
                var listeneer = listeneers[index];
                listeneer.Push(message, source.Name);
            }
        }

    }
}