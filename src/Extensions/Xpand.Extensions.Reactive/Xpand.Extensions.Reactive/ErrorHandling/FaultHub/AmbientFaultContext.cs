using System;
using System.Collections.Generic;
using System.Linq;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub{
    public record AmbientFaultContext {
        public IReadOnlyList<string> Tags { get; init; }
        public IReadOnlyList<LogicalStackFrame> LogicalStackTrace { get; init; }
        public object[] UserContext { get; init; }
        
        public virtual bool Equals(AmbientFaultContext other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(InnerContext, other.InnerContext) &&
                   string.Equals(BoundaryName, other.BoundaryName) &&
                   (LogicalStackTrace ?? Enumerable.Empty<LogicalStackFrame>()).SequenceEqual(other.LogicalStackTrace ?? Enumerable.Empty<LogicalStackFrame>()) &&
                   (UserContext ?? Enumerable.Empty<object>()).SequenceEqual(other.UserContext ?? Enumerable.Empty<object>())&&
                   (Tags??Enumerable.Empty<object>()).SequenceEqual(other.Tags ?? Enumerable.Empty<object>());
        }

        public override int GetHashCode() {
            var hashCode = new HashCode();
            hashCode.Add(InnerContext);
            hashCode.Add(BoundaryName);
            LogicalStackTrace?.Do(hashCode.Add).Enumerate();
            UserContext?.Do(hashCode.Add).Enumerate();
            Tags?.Do(hashCode.Add).Enumerate();
            return hashCode.ToHashCode();
        }

        public AmbientFaultContext InnerContext { get; init; }
        public object Name => BoundaryName ?? UserContext?.FirstOrDefault()?.ToString() ?? "Unknown";
        public string BoundaryName { get; init; }
        
    }
    
    public readonly struct LogicalStackFrame(string memberName, string filePath, int lineNumber, params object[] context) : IEquatable<LogicalStackFrame> {
        public bool Equals(LogicalStackFrame other) 
            => MemberName == other.MemberName && FilePath == other.FilePath && (Context ?? []).SequenceEqual(other.Context ?? []);
        
        public override bool Equals(object obj) 
            => obj is LogicalStackFrame other && Equals(other);
        
        public override int GetHashCode() {
            var hashCode = new HashCode();
            hashCode.Add(MemberName);
            hashCode.Add(FilePath);
            context?.Do(hashCode.Add).Enumerate();
            return hashCode.ToHashCode();
        }
        
        public object[] Context=> context;
        public string MemberName => memberName;

        public string FilePath => filePath;

        public int LineNumber => lineNumber;
        
        public override string ToString() {
            var validContexts = context?.Where(c => c is not null && !string.IsNullOrWhiteSpace(c.ToString())).ToArray();
            var contextPrefix = (validContexts?.Length > 0) ? $"{validContexts.JoinCommaSpace().EncloseParenthesis()} " : "";
            var cleanedMemberName = memberName.ParseMemberName();
            return $"{contextPrefix}at {cleanedMemberName} in {filePath}:line {lineNumber}";
        }
    }

}