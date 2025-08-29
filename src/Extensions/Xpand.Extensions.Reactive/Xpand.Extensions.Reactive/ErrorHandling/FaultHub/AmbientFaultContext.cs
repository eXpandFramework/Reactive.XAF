using System;
using System.Collections.Generic;
using System.Linq;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub{
    public record AmbientFaultContext {
        public IReadOnlyList<LogicalStackFrame> LogicalStackTrace { get; init; }
        public object[] UserContext { get; init; }
        
        public virtual bool Equals(AmbientFaultContext other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Equals(InnerContext, other.InnerContext) &&
                   string.Equals(BoundaryName, other.BoundaryName) &&
                   (LogicalStackTrace ?? Enumerable.Empty<LogicalStackFrame>()).SequenceEqual(other.LogicalStackTrace ?? Enumerable.Empty<LogicalStackFrame>()) &&
                   (UserContext ?? Enumerable.Empty<object>()).SequenceEqual(other.UserContext ?? Enumerable.Empty<object>());
        }

        public override int GetHashCode() {
            var hashCode = new HashCode();
            hashCode.Add(InnerContext);
            hashCode.Add(BoundaryName);

            if (LogicalStackTrace != null) {
                foreach (var frame in LogicalStackTrace) {
                    hashCode.Add(frame);
                }
            }

            if (UserContext != null) {
                foreach (var item in UserContext) {
                    hashCode.Add(item);
                }
            }
        
            return hashCode.ToHashCode();
        }

        public AmbientFaultContext InnerContext { get; init; }
        public object Name {
            get {
                var userCtx = UserContext?.FirstOrDefault()?.ToString();
                var boundary = BoundaryName;

                if (!string.IsNullOrEmpty(userCtx) && !string.IsNullOrEmpty(boundary) &&
                    userCtx.Replace(" ", "") != boundary.Replace(" ", "")) {
                    return $"{userCtx} {boundary}";
                }
                return userCtx ?? boundary ?? "Unknown";
            }
        }
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
            if (context != null) {
                foreach (var item in context) {
                    hashCode.Add(item);
                }
            }
            return hashCode.ToHashCode();
        }
        
        public object[] Context=> context;
        public string MemberName => memberName;

        public string FilePath => filePath;

        public int LineNumber => lineNumber;
        
        public override string ToString() => $"{(context.Any() ? $"{context.JoinCommaSpace().EncloseParenthesis()} " : "")}at {memberName} in {filePath}:line {lineNumber}";
    }

}