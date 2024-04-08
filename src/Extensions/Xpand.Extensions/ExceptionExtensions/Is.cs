using System;
using System.Linq;

namespace Xpand.Extensions.ExceptionExtensions {
    public static partial class ExceptionExtensions {
        public static bool Is<T>(this Exception exception)
            => exception.Yield<T>().Any();
    }
}