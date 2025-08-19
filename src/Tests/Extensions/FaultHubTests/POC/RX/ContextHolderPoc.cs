using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.ErrorHandling.FaultHub;
using Unit = System.Reactive.Unit;

namespace Xpand.Extensions.Tests.FaultHubTests.POC.RX {
    

    [TestFixture]
    public class ContextHolderPoc {
        
        private IObservable<Unit> Level1_FailingOperation(List<string> log)
            => Observable.Throw<Unit>(new InvalidOperationException("DB Failure"))
                .PushStackFrameWithHolder("Level1_FailingOperation", log);

        private IObservable<Unit> Level2_BusinessLogic(List<string> log)
            => Level1_FailingOperation(log)
                .PushStackFrameWithHolder("Level2_BusinessLogic", log);

        [Test]
        public void ContextHolderPattern_Preserves_Context_Across_Retries() {
            var executionLog = new List<string>();
            IReadOnlyList<LogicalStackFrame> finalStack = null;

            var stream = Level2_BusinessLogic(executionLog)
                .ChainFaultContextWithHolder(3, executionLog)
                .Catch((Exception ex) => {
                    if (ex.Data.Contains("stack")) finalStack = ex.Data["stack"] as IReadOnlyList<LogicalStackFrame>;
                    return Observable.Empty<Unit>();
                });

            stream.Subscribe();

            Console.WriteLine("--- Execution Log ---");
            Console.WriteLine(string.Join(Environment.NewLine, executionLog));
            Console.WriteLine("---------------------");

            
            finalStack.ShouldNotBeNull();
            finalStack.Count.ShouldBe(2);
            finalStack[1].MemberName.ShouldBe("Level2_BusinessLogic");
            finalStack[0].MemberName.ShouldBe("Level1_FailingOperation");
        }
    }

    static class PocOperators {
        private static readonly AsyncLocal<IReadOnlyList<LogicalStackFrame>> LogicalStackContext = new();

        
        private static readonly AsyncLocal<ContextHolder> CurrentContextHolder = new();
        private class ContextSavingDisposable(
            ContextHolder holder,
            IReadOnlyList<LogicalStackFrame> originalStack,
            List<string> log)
            : IDisposable {
            public void Dispose() {
                if (holder != null) {
                    var currentStack = LogicalStackContext.Value;
                    if ((currentStack?.Count ?? 0) > (holder.CapturedStack?.Count ?? 0)) {
                        log.Add($"   [Dispose] New longest stack found ({currentStack?.Count} frames). Saving to holder.");
                        holder.CapturedStack = currentStack;
                    } else {
                        log.Add($"   [Dispose] Current stack ({currentStack?.Count ?? 0} frames) is not longer than captured stack ({holder.CapturedStack?.Count ?? 0}). Not overwriting.");
                    }
                }

                log.Add(
                    $"   [Dispose] Clearing main logical stack. Restoring it to {originalStack?.Count ?? 0} frames.");
                LogicalStackContext.Value = originalStack;
            }
        }

        
        public static IObservable<T> PushStackFrameWithHolder<T>(this IObservable<T> source, string frameName,
            List<string> log) {
            return Observable.Using(
                () => {
                    var originalStack = LogicalStackContext.Value;
                    var newFrame = new LogicalStackFrame(frameName,"",0);
                    var newStack = new[] { newFrame }.Concat(originalStack ?? Enumerable.Empty<LogicalStackFrame>())
                        .ToList();
                    log.Add($"[PushStackFrame] Pushing '{frameName}'. New stack depth: {newStack.Count}.");
                    LogicalStackContext.Value = newStack;

                    var holder = CurrentContextHolder.Value;
                    log.Add(holder == null
                        ? "[PushStackFrame] No holder found."
                        : "[PushStackFrame] Found and using context holder.");
                    return new ContextSavingDisposable(holder, originalStack, log);
                },
                _ => source
            );
        }


        public static IObservable<T> ChainFaultContextWithHolder<T>(this IObservable<T> source, int retryCount,
            List<string> log) {
            return Observable.Defer(() => {
                var holder = new ContextHolder();
                var originalHolder = CurrentContextHolder.Value;
                log.Add("[ChainCtx] Defer: Creating new ContextHolder and setting it as ambient.");
                CurrentContextHolder.Value = holder;

                return source
                    .Retry(retryCount)
                    .Catch((Exception ex) => {
                        log.Add(
                            $"[ChainCtx] Catch: Reading stack from holder. It has {holder.CapturedStack?.Count ?? 0} frames.");
                        var enrichedException = new Exception("Final Error", ex) {
                            Data = {
                                ["stack"] = holder.CapturedStack
                            }
                        };
                        return Observable.Throw<T>(enrichedException);
                    })
                    .Finally(() => {
                        log.Add("[ChainCtx] Finally: Restoring original holder.");
                        CurrentContextHolder.Value = originalHolder;
                    });
            });
        }
    }

    public class ContextHolder {
        public IReadOnlyList<LogicalStackFrame> CapturedStack { get; set; }
    }
}