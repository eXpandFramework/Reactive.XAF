using System;
using System.Threading;

namespace Xpand.Extensions.DateTimeExtensions {
    public static partial class DateTimeExtensions {
        public static void Sleep(this TimeSpan timeSpan, CancellationToken token = default)
            => Thread.Sleep(timeSpan);
    }
}