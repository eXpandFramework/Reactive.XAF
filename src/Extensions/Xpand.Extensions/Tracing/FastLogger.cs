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
            isEnabled = true;
            _stringBuilder = new StringBuilder(literalLength + formattedCount * 8);
        }

        public void AppendLiteral(string value) => _stringBuilder?.Append(value);

        public void AppendFormatted<T>(T value) => _stringBuilder?.Append(value);

        internal string GetFormattedText() => _stringBuilder?.ToString() ?? string.Empty;
    }


    public static class FastLogger {
        [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")] 
        public static ConsoleColor ErrorColor = ConsoleColor.Red;
        [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")] 
        public static ConsoleColor WarningColor = ConsoleColor.DarkYellow;

        public static bool Enabled { get; set; }

        public static Action<string> Write { get; set; } = Console.WriteLine;

        public static void LogError(ref HighPerformanceLogBuilder builder) {
            Write(ErrorColor.AnsiColorize(builder.GetFormattedText()));
        }

        public static void LogWarning(ref HighPerformanceLogBuilder builder) {
            Write(WarningColor.AnsiColorize(builder.GetFormattedText()));
        }

        public static void LogFast(ref HighPerformanceLogBuilder builder) {
            if (!Enabled) return;
            Write(builder.GetFormattedText());
        }

        private static string AnsiColorize(this ConsoleColor color,string message) 
            => $"\x1b[{color.AnsiColorCode()}m{message}\x1b[0m";

        private static string AnsiColorCode(this ConsoleColor color) => color switch {
            ConsoleColor.Black => "30",
            ConsoleColor.DarkRed => "31",
            ConsoleColor.DarkGreen => "32",
            ConsoleColor.DarkYellow => "33",
            ConsoleColor.DarkBlue => "34",
            ConsoleColor.DarkMagenta => "35",
            ConsoleColor.DarkCyan => "36",
            ConsoleColor.Gray => "37",
            ConsoleColor.DarkGray => "90",
            ConsoleColor.Red => "91",
            ConsoleColor.Green => "92",
            ConsoleColor.Yellow => "93",
            ConsoleColor.Blue => "94",
            ConsoleColor.Magenta => "95",
            ConsoleColor.Cyan => "96",
            ConsoleColor.White => "97",
            _ => "37"
        };
    }
}