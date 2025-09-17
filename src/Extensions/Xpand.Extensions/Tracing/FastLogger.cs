using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;


namespace Xpand.Extensions.Tracing{
    [InterpolatedStringHandler]
    [SuppressMessage("ReSharper", "StructCanBeMadeReadOnly")]
    public ref struct HighPerformanceLogBuilder {
        private readonly StringBuilder _stringBuilder;

        public HighPerformanceLogBuilder(int literalLength, int formattedCount, out bool isEnabled) {
            isEnabled = FastLogger.Enabled;
            _stringBuilder = isEnabled ? new StringBuilder(literalLength + formattedCount * 8) : null;
        }

        public void AppendLiteral(string value) => _stringBuilder?.Append(value);

        public void AppendFormatted<T>(T value) => _stringBuilder?.Append(value);

        internal string GetFormattedText() => _stringBuilder?.ToString() ?? string.Empty;
    }

    
    public static class FastLogger {
        public static bool Enabled { get; set; }
        public static void LogFast(ref HighPerformanceLogBuilder builder) {
            if (!Enabled) return;
            Console.WriteLine(builder.GetFormattedText());
        }
        
    }
}