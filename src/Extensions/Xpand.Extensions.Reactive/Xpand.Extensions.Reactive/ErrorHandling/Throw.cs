using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.ExceptionExtensions;
using Xpand.Extensions.Reactive.Combine;

namespace Xpand.Extensions.Reactive.ErrorHandling {
    public static partial class ErrorHandling {
        const string Key = "Origin";
        static readonly string[] InternalNs = ["System.Reactive."];
        static readonly ConcurrentDictionary<string, string> StackTraceStringCache = new();

        private static string GetOrAddFilteredStackTraceString() {
            var keyTrace = new StackTrace(false);
            var frames = keyTrace.GetFrames();

            StackFrame originFrame = null;
            var ourNamespace = typeof(ErrorHandling).Namespace;

            foreach (var frame in frames) {
                var method = frame.GetMethod();
                if (method?.DeclaringType != null && method.DeclaringType.Namespace != ourNamespace) {
                    originFrame = frame;
                    break;
                }
            }

            if (originFrame?.GetMethod() == null) {
                // Cannot determine a reliable key, so return an unfiltered trace without caching.
                return new StackTrace(true).ToString();
            }

            var originMethod = originFrame.GetMethod();
            var key = $"{originMethod?.DeclaringType?.AssemblyQualifiedName}:{originMethod?.MetadataToken}:{originFrame.GetILOffset()}";

            // Use the cache. The factory function is only executed if the key is not found.
            return StackTraceStringCache.GetOrAdd(key, _ => {
                // This is the expensive part that runs only on a cache miss.
                var fullTrace = new StackTrace(true);
                var traceFrames = fullTrace.GetFrames();

                var filteredFrames = traceFrames.Where(frame => {
                    var method = frame.GetMethod();
                    if (method?.DeclaringType == null) return false;
            
                    var ns = method.DeclaringType.Namespace;
                    return ns != null 
                           && ns != ourNamespace 
                           && InternalNs.All(internalNs => !ns.StartsWith(internalNs));
                });

                var stringBuilder = new System.Text.StringBuilder();
                foreach (var frame in filteredFrames) {
                    stringBuilder.AppendLine($"   at {frame.ToString().Trim()}");
                }
                return stringBuilder.ToString();
            });
        }
        public static Exception TagOrigin(this Exception ex)
            => ex.AccessData(data => {
                if (data.Contains(Key)) return ex;
                data[Key] = GetOrAddFilteredStackTraceString();
                return ex;
            });
        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "ConvertToLambdaExpression")]
        public static IObservable<T> Throw<T>(this Exception ex) 
            => Observable.Defer(() => Observable.Throw<T>(ex.TagOrigin()));
        
        public static IObservable<T> ThrowOnNext<T>(this IObservable<T> source, [CallerMemberName] string caller = "")
            => source.SelectMany(arg => new InvalidOperationException($"{arg} - {caller}").Throw<T>());

        public static IObservable<Unit> ThrowUnit(this Exception exception)
            => exception.Throw<Unit>();
        
        public static IObservable<object> ThrowObject(this Exception exception)
            => exception.Throw<object>();
        
        public static IObservable<T> ThrowIfEmpty<T>(this IObservable<T> source, [CallerMemberName] string caller = "")
            => source.SwitchIfEmpty(Observable.Defer(() => new SequenceIsEmptyException($"source is empty {caller}").Throw<T>()));
        
        public static IObservable<T> ThrowIfEmpty<T>(this IObservable<T> source, Exception exception) 
            => source.SwitchIfEmpty(exception.Throw<T>());
    }

    public class SequenceIsEmptyException(string message) : Exception(message);
}