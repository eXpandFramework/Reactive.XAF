using System;
using System.Linq;

namespace Xpand.Extensions.ExceptionExtensions {
    public static partial class ExceptionExtensions {
        public static bool Has<T>(this Exception exception)
            => exception.Flatten().OfType<T>().Any();
    }
}