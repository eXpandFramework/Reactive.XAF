using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Numeric;

namespace Xpand.Extensions.ProcessExtensions {
    public static partial class ProcessExtensions {
        public static IntPtr WaitForMainWindowHandle(this Process process, TimeSpan? timeout = null) {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < (timeout ?? 5.Seconds())) {
                if (process.MainWindowHandle != IntPtr.Zero)
                    return process.MainWindowHandle;
                Thread.Sleep(200);
                process.Refresh();
            }

            return IntPtr.Zero;
        }
    }
}