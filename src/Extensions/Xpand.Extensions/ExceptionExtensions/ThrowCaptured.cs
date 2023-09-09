using System;

namespace Xpand.Extensions.ExceptionExtensions {
    public static partial class ExceptionExtensions {
        public static void ThrowCaptured(this Exception exception)
            => exception.Capture().Throw();
    }
}