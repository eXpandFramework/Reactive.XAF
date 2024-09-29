using System;
using System.Threading;
using System.Threading.Tasks;

namespace Xpand.Extensions.DateTimeExtensions {
    public static partial class DateTimeExtensions {
        public static Task Delay(this TimeSpan timeSpan,CancellationToken token=default) 
            => Task.Delay(timeSpan, token);
        public static Task Delay(this int millisecond,CancellationToken token=default) 
            => Task.Delay(millisecond, token);
    }
}