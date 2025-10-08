using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Xpand.Extensions.Reactive.Relay;

namespace Xpand.Extensions.Tests.FaultHubTests.NewDirectory1 {

    public static class ObservableExtensions {
        private const string LogicalStackKey = "LogicalRxStack";
        public static IObservable<T> CatchWithContext<T, TException>(this IObservable<T> source, Func<TException, IObservable<T>> handler) where TException : Exception {
            return Observable.Create<T>(observer => {
                var subscription = new System.Reactive.Disposables.SerialDisposable();
                
                var sourceSub = source.Subscribe(
                    observer.OnNext,
                    ex => {
                        if (ex is TException exception) {
                            var pollutedStack = FaultHub.LogicalStackContext.Value;
                            try {
                                FaultHub.LogicalStackContext.Value = null;
                                LogFast($"CatchWithContext: Executing recovery handler in a clean context. Polluted depth was: {pollutedStack?.Count()}");
                                var recoveryStream = handler(exception);
                                subscription.Disposable = recoveryStream.Subscribe(observer);
                            }
                            catch (Exception handlerEx) {
                                observer.OnError(handlerEx);
                            }
                            finally {
                                FaultHub.LogicalStackContext.Value = pollutedStack;
                            }
                        }
                        else {
                            observer.OnError(ex);
                        }
                    },
                    observer.OnCompleted
                );
                subscription.Disposable = sourceSub;
                return subscription;
            });
        }
        public static IObservable<T> PushFrame<T>(this IObservable<T> source, object[] context = null,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0) {
            var frame = new LogicalStackFrame(memberName, filePath, lineNumber, context);
            LogFast($"Created frame: {frame.MemberName} @ L{frame.LineNumber}");
            return Observable.Create<T>(observer => {
                LogFast($"Subscribing for frame: {frame.MemberName} @ L{frame.LineNumber}");

                return source.Subscribe(
                    value => {
                        using (new StackManager(frame)) {
                            observer.OnNext(value);
                        }
                    },
                    ex => {
                        using (new StackManager(frame)) {
                            AttachStackToException(ex);
                            observer.OnError(ex);
                        }
                    },
                    () => {
                        using (new StackManager(frame)) {
                            observer.OnCompleted();
                        }
                    }
                );
            });
        }

        private readonly struct StackManager : IDisposable {
            private readonly IImmutableStack<LogicalStackFrame> _originalStack;

            public StackManager(LogicalStackFrame frame) {
                _originalStack = FaultHub.LogicalStackContext.Value;
                var newStack = (_originalStack ?? ImmutableStack<LogicalStackFrame>.Empty).Push(frame);
                FaultHub.LogicalStackContext.Value = newStack;
                LogFast($"Pushed '{frame.MemberName}'. Thread: {Thread.CurrentThread.ManagedThreadId}. New depth: {newStack.Count()}.");
            }

            public void Dispose() {
                FaultHub.LogicalStackContext.Value = _originalStack;
            }
        }

        public static ImmutableStack<LogicalStackFrame> GetLogicalStack(this Exception ex) {
            if (ex?.Data.Contains(LogicalStackKey) == true &&
                ex.Data[LogicalStackKey] is ImmutableStack<LogicalStackFrame> stack) return stack;
            return ImmutableStack<LogicalStackFrame>.Empty;
        }

        internal static void AttachStackToException(Exception exception) {
            if (exception != null && !exception.Data.Contains(LogicalStackKey)) {
                LogFast($"Attaching stack to exception. Depth: {FaultHub.LogicalStackContext.Value?.Count()}.");
                exception.Data[LogicalStackKey] = FaultHub.LogicalStackContext.Value;
            }
        }
    }
}