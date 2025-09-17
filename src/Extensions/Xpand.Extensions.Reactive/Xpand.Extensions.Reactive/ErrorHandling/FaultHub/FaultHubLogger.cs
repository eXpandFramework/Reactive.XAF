using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using static Xpand.Extensions.Reactive.ErrorHandling.FaultHub.FaultHub;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub{
    using System.Runtime.CompilerServices;
    using System.Text;

    [InterpolatedStringHandler]
    [SuppressMessage("ReSharper", "StructCanBeMadeReadOnly")]
    public ref struct HighPerformanceLogBuilder {
        private readonly StringBuilder _stringBuilder;

        public HighPerformanceLogBuilder(int literalLength, int formattedCount, out bool isEnabled) {
            isEnabled = Logging;
            _stringBuilder = isEnabled ? new StringBuilder(literalLength + formattedCount * 8) : null;
        }

        public void AppendLiteral(string value) => _stringBuilder?.Append(value);

        public void AppendFormatted<T>(T value) => _stringBuilder?.Append(value);

        internal string GetFormattedText() => _stringBuilder?.ToString() ?? string.Empty;
    }

    
    public static class FaultHubLogger {
        public static void LogFast(ref HighPerformanceLogBuilder builder) {
            if (Logging) {
                LogWrite(builder.GetFormattedText());
            }
        }

        public static Action<string> LogWrite { get; set; } = s => {
            if (Debugger.IsAttached)
                Debug.WriteLine(s);
            else
                Console.WriteLine(s);
        };

        // public static void Log(Func<string> messageSelector) {
        //     LogFast($"");
        //     if (Logging) {
        //         if (Debugger.IsAttached) {
        //             Debug.WriteLine(messageSelector());
        //         }
        //         else {
        //             Console.WriteLine(messageSelector());
        //         }
        //         
        //     };
        // }
    }
}