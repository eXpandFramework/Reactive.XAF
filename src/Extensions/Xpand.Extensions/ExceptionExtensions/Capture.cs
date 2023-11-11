using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.ExceptionExtensions {
    public static partial class ExceptionExtensions {
        public static ExceptionDispatchInfo Capture(this Exception exception)
            => ExceptionDispatchInfo.Capture(exception);
    }
}