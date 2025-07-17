using System;
using System.Runtime.ExceptionServices;

namespace Xpand.Extensions.ExceptionExtensions {
    public static partial class ExceptionExtensions {
        public static ExceptionDispatchInfo Capture(this Exception exception)
            => ExceptionDispatchInfo.Capture(exception);
    }
}