using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using Xpand.Extensions.Reactive.Relay;

namespace Xpand.Extensions.Tests.FaultHubTests.ProofOfConcepts{

        static class FixedPocOperators {
            internal static readonly AsyncLocal<IReadOnlyList<LogicalStackFrame>> LogicalStackContext = new();
            internal static readonly AsyncLocal<ContextHolder> CurrentContextHolder = new();
            public static IObservable<T> PushStackFrame_Fixed<T>(this IObservable<T> source, string frameName,
                List<string> log) {
                return Observable.Defer(() => {
                    var originalStack = LogicalStackContext.Value;
                    var newFrame = new LogicalStackFrame(frameName,"",0);
                    var newStack = new[] { newFrame }.Concat(originalStack ?? Enumerable.Empty<LogicalStackFrame>()).ToList();
                    LogicalStackContext.Value = newStack;

                    return source.Materialize().Do(notification => {
                            if (notification.Kind == NotificationKind.OnError) {
                                var holder = CurrentContextHolder.Value;
                                if (holder != null) {
                                    var currentStack = LogicalStackContext.Value;
                                    if ((currentStack?.Count ?? 0) > (holder.CapturedStack?.Count ?? 0)) {
                                        log.Add(
                                            $"   [Push-FIXED] OnError detected. Saving stack ({currentStack?.Count} frames) to holder.");
                                        holder.CapturedStack = currentStack;
                                    }
                                }
                            }
                        }).Dematerialize()
                        .Finally(() => { LogicalStackContext.Value = originalStack; });
                });
            }

            public static IObservable<T> ChainFaultContext_Fixed<T>(this IObservable<T> source, int retryCount,
                List<string> log) =>
                Observable.Defer(() => {
                    var holder = new ContextHolder();
                    var originalHolder = CurrentContextHolder.Value;
                    CurrentContextHolder.Value = holder;
                    return source.Retry(retryCount).Catch((Exception ex) => {
                        log.Add(
                            $"[ChainCtx-FIXED] Catch: Reading stack from holder. It has {holder.CapturedStack?.Count ?? 0} frames.");
                        var enrichedException = new Exception("Final Error", ex) {
                            Data = {
                                ["stack"] = holder.CapturedStack
                            }
                        };
                        return Observable.Throw<T>(enrichedException);
                    }).Finally(() => { CurrentContextHolder.Value = originalHolder; });
                });
        }
}