using System;
using static Xpand.Extensions.Reactive.ErrorHandling.FaultHub.FaultHub;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub{
    internal static class FaultHubLogger {
        public static void Log(Func<string> messageSelector) {
            if (Logging) Console.WriteLine(messageSelector());
        }
    }
}