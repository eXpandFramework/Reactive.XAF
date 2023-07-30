using System;
using System.Runtime.ExceptionServices;

namespace Xpand.Extensions.ReflectionExtensions {
    public partial class ReflectionExtensions {
        public static void ThrowCaptured(this Exception exception)
            => ExceptionDispatchInfo.Capture(exception).Throw();
    }
}