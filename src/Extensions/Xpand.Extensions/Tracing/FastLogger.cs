using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Xpand.Extensions.Tracing{
    [InterpolatedStringHandler]
    [SuppressMessage("ReSharper", "StructCanBeMadeReadOnly")]
    public ref struct HighPerformanceLogBuilder {
        private readonly StringBuilder _stringBuilder;
        public string MemberName { get; }
        public string FilePath { get; }
        

        public HighPerformanceLogBuilder(int literalLength, int formattedCount, out bool isEnabled,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "") {
            isEnabled = Enabled;
            if (!isEnabled) {
                MemberName = null;
                FilePath = null;
                return;
            }
            _stringBuilder = new StringBuilder(literalLength + formattedCount * 8);
            MemberName = memberName;
            FilePath = filePath;
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

        private static string FormatMessage(ref HighPerformanceLogBuilder builder) {
            var message = builder.GetFormattedText();
            return !string.IsNullOrEmpty(builder.FilePath)
                ? $"{Path.GetFileNameWithoutExtension(builder.FilePath)} - {builder.MemberName} | {message}"
                : message;
        }
        private static readonly AsyncLocal<List<Func<(string method, string path), bool>>> Predicates = new();

        public static IDisposable Filter(Func<(string method, string path), bool> predicate) {
            Predicates.Value ??= new List<Func<(string method, string path), bool>>();
            Predicates.Value.Add(predicate);
            return new FilterScope(predicate);
        }
        
        private readonly struct FilterScope(Func<(string method, string path), bool> predicate) : IDisposable {
            public void Dispose() => Predicates.Value?.Remove(predicate);
        }
        private static bool ShouldLog(ref HighPerformanceLogBuilder builder) {
            if (Predicates.Value == null || !Predicates.Value.Any()) return true;
            foreach (var predicate in Predicates.Value) {
                if (predicate((builder.MemberName, builder.FilePath))) continue;
                return false;
            }
            return true;
        }

        public static void LogError(ref HighPerformanceLogBuilder builder) {
            if (!ShouldLog(ref builder)) return;
            if (builder.GetFormattedText().StartsWith("[")) return;
            Write(ErrorColor.AnsiColorize(FormatMessage(ref builder)));
        }

        public static void LogWarning(ref HighPerformanceLogBuilder builder) {
            if (!ShouldLog(ref builder)) return;
            if (builder.GetFormattedText().StartsWith("[")) return;
            Write(WarningColor.AnsiColorize(FormatMessage(ref builder)));
        }
        
        public static void LogFast(ref HighPerformanceLogBuilder builder) {
            if (!Enabled) return;
            if (!ShouldLog(ref builder)) return;
            if (builder.GetFormattedText().StartsWith("[")) return;
            Write(FormatMessage(ref builder));
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