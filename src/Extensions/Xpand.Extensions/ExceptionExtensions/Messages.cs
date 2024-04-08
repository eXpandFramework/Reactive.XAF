using System;
using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.ExceptionExtensions {
    public static partial class ExceptionExtensions {
        public static IEnumerable<string> Messages<T>(this Exception exception)
            => exception.Yield<T>().Cast<Exception>().Select(arg => arg.Message);
    }
}