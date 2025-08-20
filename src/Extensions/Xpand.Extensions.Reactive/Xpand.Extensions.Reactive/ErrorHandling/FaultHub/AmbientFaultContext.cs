using System;
using System.Collections.Generic;
using System.Linq;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub{
    public record AmbientFaultContext {
        public IReadOnlyList<LogicalStackFrame> LogicalStackTrace { get; init; }
        public object[] CustomContext { get; init; }
        public AmbientFaultContext InnerContext { get; init; }
        public object Name => CustomContext.FirstOrDefault() ?? "Unknown";
    }
    
    public readonly struct LogicalStackFrame(
        string memberName,
        string filePath,
        int lineNumber,
        params object[] context)
        : IEquatable<LogicalStackFrame> {
        public bool Equals(LogicalStackFrame other) 
            => MemberName == other.MemberName && FilePath == other.FilePath && (Context ?? []).SequenceEqual(other.Context ?? []);
        
        public override bool Equals(object obj) 
            => obj is LogicalStackFrame other && Equals(other);
        
        public override int GetHashCode() 
            => HashCode.Combine(MemberName, FilePath);
        public object[] Context=> context;
        public string MemberName => memberName;

        public string FilePath => filePath;

        public int LineNumber => lineNumber;

        public override string ToString() => $"{context.JoinCommaSpace().EncloseParenthesis()} at {memberName} in {filePath}:line {lineNumber}";
    }

}