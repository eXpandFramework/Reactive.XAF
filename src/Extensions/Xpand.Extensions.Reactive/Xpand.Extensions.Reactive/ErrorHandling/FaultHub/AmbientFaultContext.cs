using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub{
    public record AmbientFaultContext {
        public IReadOnlyList<LogicalStackFrame> LogicalStackTrace { get; init; }
        public IReadOnlyList<string> CustomContext { get; init; }
        public AmbientFaultContext InnerContext { get; init; }
        public object Name => CustomContext.FirstOrDefault() ?? "Unknown";
    }
    
    public readonly struct LogicalStackFrame(string memberName, string filePath, int lineNumber) {
        public string MemberName => memberName;

        public string FilePath => filePath;

        public int LineNumber => lineNumber;

        public override string ToString() => $"at {memberName} in {filePath}:line {lineNumber}";
    }

}