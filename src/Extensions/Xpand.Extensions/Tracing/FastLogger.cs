using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Xpand.Extensions.Colors;

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
            if (!Enabled) return;
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
        private static readonly AsyncLocal<List<Func<(FastLogLevel level, string method, string path), bool>>> Predicates = new();

        public static Func<(FastLogLevel level, string method, string path), bool> Or(params Func<(FastLogLevel level, string method, string path), bool>[] predicates) 
            => context => predicates.Any(predicate => predicate(context));

        public static bool Contains(this HashSet<string> types, (FastLogLevel level, string method, string path) t) 
            => types.Contains(Path.GetFileNameWithoutExtension(t.path));


        public static IDisposable LogFastFilter(Func<(FastLogLevel level, string method, string path), bool> predicate, params Type[] types) {
            var hashSet = types.Select(type => type.Name).ToHashSet();
            return LogFastFilter(Or(predicate, t => hashSet.Contains(t)));
        }

        public static IDisposable LogFastFilter(Func<(FastLogLevel level, string method, string path), bool> predicate) {
            Predicates.Value ??= new List<Func<(FastLogLevel level, string method, string path), bool>>();
            Predicates.Value.Add(predicate);
            return new FilterScope(predicate);
        }
        
        private readonly struct FilterScope(Func<(FastLogLevel level, string method, string path), bool> predicate) : IDisposable {
            public void Dispose() => Predicates.Value?.Remove(predicate);
        }
        private static bool ShouldLog(FastLogLevel level, ref HighPerformanceLogBuilder builder) {
            if (Predicates.Value == null || !Predicates.Value.Any()) return true;
            foreach (var predicate in Predicates.Value) {
                if (predicate((level, builder.MemberName, builder.FilePath))) continue;
                return false;
            }
            return true;
        }

        public static void FastLog(this Exception exception) => LogError($"{exception}");

        public static void LogError(ref HighPerformanceLogBuilder builder) {
            if (!Enabled)return;
            if (!ShouldLog(FastLogLevel.Error, ref builder)) return;
            Write(ErrorColor.AnsiColorize(FormatMessage(ref builder)));
        }

        public static void LogWarning(ref HighPerformanceLogBuilder builder) {
            if (!Enabled)return;
            if (!ShouldLog(FastLogLevel.Warning, ref builder)) return;
            Write(WarningColor.AnsiColorize(FormatMessage(ref builder)));
        }

        public static void LogFast(ref HighPerformanceLogBuilder builder) {
            if (!Enabled)return;
            if (!ShouldLog(FastLogLevel.Info, ref builder)) return;
            Write(FormatMessage(ref builder));
        }

        private static string AnsiColorize(this ConsoleColor color,string message) 
            => $"\x1b[{color.ToAnsiColorCode()}m{message}\x1b[0m";
        
        
        
    }
    
    public enum FastLogLevel {
        Info,
        Warning,
        Error
    }
    
}