using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.Reactive.Relay;

namespace Xpand.Extensions.Tests.FaultHubTests.NewDirectory1 {
    [TestFixture]
    public class CausalityGraphIsolationTests : FaultHubTestBase {
        static CausalityGraphIsolationTests() => Enabled = true;

        [Test]
        public async Task Should_Capture_Nested_Stack_For_Failing_Inner_Stream() {
            var tcs = new TaskCompletionSource<Exception>();

            var streamA = Observable.Interval(TimeSpan.FromMilliseconds(10))
                .PushFrame();
            using var subscriptionA = streamA.Subscribe();

            int streamBLine;
            var streamCLine = 0;

            var streamB = Observable.Return(0L)
                .PushFrame(filePath: "LogicalStackFrameTests.cs", lineNumber: streamBLine = GetLineNumber())
                .SelectMany(_ =>
                {
                    var streamC = Observable.Throw<long>(new InvalidOperationException("Stream C Error"))
                        .PushFrame(filePath: "LogicalStackFrameTests.cs", lineNumber: streamCLine = GetLineNumber());
                    return streamC;
                });

            using var subscriptionB = streamB.Subscribe(
                _ => { },
                ex => tcs.SetResult(ex),
                () => { }
            );

            var caughtException = await tcs.Task;

            caughtException.ShouldNotBeNull();
            var stack = caughtException.GetLogicalStack();

            stack.IsEmpty.ShouldBeFalse();
            stack.Count().ShouldBe(2);

            var streamCFrame = stack.Peek();
            var rest = stack.Pop();
            var streamBFrame = rest.Peek();

            streamCFrame.FilePath.ShouldBe("LogicalStackFrameTests.cs");
            streamCFrame.LineNumber.ShouldBe(streamCLine);
            streamCFrame.MemberName.ShouldBe(nameof(Should_Capture_Nested_Stack_For_Failing_Inner_Stream));

            streamBFrame.FilePath.ShouldBe("LogicalStackFrameTests.cs");
            streamBFrame.LineNumber.ShouldBe(streamBLine);
            streamBFrame.MemberName.ShouldBe(nameof(Should_Capture_Nested_Stack_For_Failing_Inner_Stream));
        }

    [Test]
    public async Task Should_Maintain_Stack_Isolation_For_Concurrent_Streams() {
        var tcs = new TaskCompletionSource<Exception>();

        var streamA = Observable.Return(0L)
            .PushFrame()
            .SelectMany(_ => Observable.Return(0L).Delay(TimeSpan.FromSeconds(1)).PushFrame());

        int streamBLine;
        var streamDLine = 0;
        var streamB = Observable.Return(0L)
            .PushFrame(filePath: "LogicalStackFrameTests.cs", lineNumber: streamBLine = GetLineNumber())
            .SelectMany(_ => Observable.Throw<long>(new InvalidOperationException("Stream D Error"))
                .PushFrame(filePath: "LogicalStackFrameTests.cs", lineNumber: streamDLine = GetLineNumber()));

        using var subscriptionA = streamA.Subscribe();
        using var subscriptionB = streamB.Subscribe(_ => { },
            ex => tcs.SetResult(ex)
        );

        var caughtException = await tcs.Task;

        caughtException.ShouldNotBeNull();
        var stack = caughtException.GetLogicalStack();

        stack.IsEmpty.ShouldBeFalse();
        stack.Count().ShouldBe(2);

        var streamDFrame = stack.Peek();
        var rest = stack.Pop();
        var streamBFrame = rest.Peek();

        streamDFrame.FilePath.ShouldBe("LogicalStackFrameTests.cs");
        streamDFrame.LineNumber.ShouldBe(streamDLine);

        streamBFrame.FilePath.ShouldBe("LogicalStackFrameTests.cs");
        streamBFrame.LineNumber.ShouldBe(streamBLine);
    }
    
    [Test]

    public async Task Should_Preserve_Stack_Across_Scheduler_Boundaries()
    {
        var tcs = new TaskCompletionSource<Exception>();
        int frame1Line;
        int frame2Line;
        int frame3Line;

        var stream = Observable.Return(0L)
            
            .SubscribeOn(TaskPoolScheduler.Default)
            .PushFrame(filePath: "LogicalStackFrameTests.cs", lineNumber: frame1Line = GetLineNumber())
            
            .ObserveOn(TaskPoolScheduler.Default)
            .PushFrame(filePath: "LogicalStackFrameTests.cs", lineNumber: frame2Line = GetLineNumber())
            .SelectMany(_ => Observable.Throw<long>(new InvalidOperationException("Scheduler Error")))
            .PushFrame(filePath: "LogicalStackFrameTests.cs", lineNumber: frame3Line = GetLineNumber());

        using var subscription = stream.Subscribe(
            _ => { },
            ex => tcs.SetResult(ex)
        );

        var caughtException = await tcs.Task;

        caughtException.ShouldNotBeNull();
        var stack = caughtException.GetLogicalStack();

        stack.IsEmpty.ShouldBeFalse();
        stack.Count().ShouldBe(3);

        var frame3 = stack.Peek();
        var rest2 = stack.Pop();
        var frame2 = rest2.Peek();
        var rest1 = rest2.Pop();
        var frame1 = rest1.Peek();

        frame3.LineNumber.ShouldBe(frame3Line);
        frame2.LineNumber.ShouldBe(frame2Line);
        frame1.LineNumber.ShouldBe(frame1Line);
    }
    
    [Test]
    public async Task Should_Preserve_Stack_For_Failing_Inner_Stream_Across_ObserveOn() {
        var tcs = new TaskCompletionSource<Exception>();
        int frame1Line;
        var frame2Line = 0;

        var stream = Observable.Return(0L)
            .PushFrame(filePath: "LogicalStackFrameTests.cs", lineNumber: frame1Line = GetLineNumber())
            .ObserveOn(Scheduler.Default)
            .SelectMany(_ => {
                var stackOnEnter = FaultHub.LogicalStackContext.Value;
                return Observable.Throw<long>(new InvalidOperationException("Inner Stream Error"))
                    .PushFrame(filePath: "LogicalStackFrameTests.cs", lineNumber: frame2Line = GetLineNumber())
                    .CatchWithContext<long, Exception>(ex => {
                        if (!ex.Data.Contains("LogicalRxStack")) {
                            ex.Data["LogicalRxStack"] = stackOnEnter;
                        }
                        return Observable.Throw<long>(ex);
                    });
            });

        using var subscription = stream.Subscribe(
            _ => { },
            ex => tcs.SetResult(ex)
        );

        var caughtException = await tcs.Task;

        caughtException.ShouldNotBeNull();
        var stack = caughtException.GetLogicalStack();

        stack.IsEmpty.ShouldBeFalse();
        stack.Count().ShouldBe(2);

        var frame2 = stack.Peek();
        var rest1 = stack.Pop();
        var frame1 = rest1.Peek();

        frame2.LineNumber.ShouldBe(frame2Line);
        frame1.LineNumber.ShouldBe(frame1Line);
    }
    
[Test]
    public async Task Should_Discard_Failed_Process_Stack_And_Preserve_Recovery_Stack_With_Catch() {
        var tcs = new TaskCompletionSource<Exception>();
        int frameALine;
        var frameBLine = 0;
        var frameCLine = 0;

        var stream = Observable.Return(0L)
            .PushFrame(filePath: "LogicalStackFrameTests.cs", lineNumber: frameALine = GetLineNumber())
            .SelectMany(_ => 
                    Observable.Throw<long>(new InvalidOperationException("Inner Failing Process"))
                        .PushFrame(filePath: "LogicalStackFrameTests.cs", lineNumber: frameBLine = GetLineNumber()))
            .CatchWithContext<long, Exception>(_ => Observable.Throw<long>(new InvalidOperationException("Recovery Process Error"))
                .PushFrame(filePath: "LogicalStackFrameTests.cs", lineNumber: frameCLine = GetLineNumber()));

        using var subscription = stream.Subscribe(
            _ => { },
            ex => tcs.SetResult(ex)
        );

        var caughtException = await tcs.Task;

        caughtException.ShouldNotBeNull();
        caughtException.Message.ShouldBe("Recovery Process Error");
        var stack = caughtException.GetLogicalStack();

        stack.IsEmpty.ShouldBeFalse();
        stack.Count().ShouldBe(1); 

        stack.ShouldNotContain(frame => frame.LineNumber == frameALine, "Frame A from the parent process should have been discarded for isolation.");
        stack.ShouldNotContain(frame => frame.LineNumber == frameBLine, "Frame B from the failed process should have been discarded.");

        var frameC = stack.Peek();
        frameC.LineNumber.ShouldBe(frameCLine);    }
    
    [Test]
    public async Task Should_Clear_Stack_Between_Retry_Attempts() {
        var tcs = new TaskCompletionSource<Exception>();
        var frameLine = 0;
        var attemptCount = 0;

        var stream = Observable.Defer(() => {
                attemptCount++;
                return Observable.Return(0L)
                    .SelectMany(_ => Observable.Throw<long>(new InvalidOperationException($"Attempt {attemptCount} failed")))
                    .PushFrame(filePath: "LogicalStackFrameTests.cs", lineNumber: frameLine = GetLineNumber());
            })
            .Retry(2)
            .CatchWithContext<long, Exception>(ex => {
                tcs.SetResult(ex);
                return Observable.Empty<long>();
            });

        using var subscription = stream.Subscribe();

        var caughtException = await tcs.Task;

        caughtException.ShouldNotBeNull();
        var stack = caughtException.GetLogicalStack();

        stack.IsEmpty.ShouldBeFalse("The stack should contain the frame from the final failed attempt.");
        stack.Count().ShouldBe(1, "The stack should only contain one frame from the final attempt, not frames from previous failed attempts.");
        
        var frame = stack.Peek();
        frame.LineNumber.ShouldBe(frameLine);
    }
    
    [Test]
    public async Task Should_Maintain_Stack_Isolation_For_Multiple_Subscribers_To_Hot_Observable() {
        var tcsA = new TaskCompletionSource<Exception>();
        var tcsB = new TaskCompletionSource<Exception>();
        int frameALine;
        int frameBLine;

        var source = new System.Reactive.Subjects.ReplaySubject<long>(1);
        var hotSource = source.Publish();

        var streamA = hotSource
            .SelectMany(_ => Observable.Throw<long>(new InvalidOperationException("Error A")))
            .PushFrame(filePath: "LogicalStackFrameTests.cs", lineNumber: frameALine = GetLineNumber());

        var streamB = hotSource
            .SelectMany(_ => Observable.Throw<long>(new InvalidOperationException("Error B")))
            .PushFrame(filePath: "LogicalStackFrameTests.cs", lineNumber: frameBLine = GetLineNumber());

        using var subA = streamA.Subscribe(_ => { }, ex => tcsA.SetResult(ex));
        using var subB = streamB.Subscribe(_ => { }, ex => tcsB.SetResult(ex));

        hotSource.Connect();
        source.OnNext(1);

        await Task.WhenAll(tcsA.Task, tcsB.Task);

        var exceptionA = tcsA.Task.Result;
        var exceptionB = tcsB.Task.Result;

        exceptionA.ShouldNotBeNull();
        var stackA = exceptionA.GetLogicalStack();
        stackA.IsEmpty.ShouldBeFalse();
        stackA.Count().ShouldBe(1);
        stackA.Peek().LineNumber.ShouldBe(frameALine);
        stackA.ShouldNotContain(frame => frame.LineNumber == frameBLine, "Stack A should not contain Frame B.");

        exceptionB.ShouldNotBeNull();
        var stackB = exceptionB.GetLogicalStack();
        stackB.IsEmpty.ShouldBeFalse();
        stackB.Count().ShouldBe(1);
        stackB.Peek().LineNumber.ShouldBe(frameBLine);
        stackB.ShouldNotContain(frame => frame.LineNumber == frameALine, "Stack B should not contain Frame A.");
    }
    
    [Test]
    public async Task Should_Preserve_Stack_Across_Time_Based_Delay_Operator_With_Real_Scheduler() {
        
        var tcs = new TaskCompletionSource<Exception>();
        int frameALine;
        int frameBLine;

        var stream = Observable.Return(0L)
            
            .Delay(TimeSpan.FromMilliseconds(50),Scheduler.Default)
            .PushFrame(filePath: "LogicalStackFrameTests.cs", lineNumber: frameALine = GetLineNumber())
            .SelectMany(_ => Observable.Throw<long>(new InvalidOperationException("Error after delay")))
            .PushFrame(filePath: "LogicalStackFrameTests.cs", lineNumber: frameBLine = GetLineNumber());

        using var subscription = stream.Subscribe(
            _ => { },
            ex => tcs.SetResult(ex)
        );

        var caughtException = await tcs.Task;

        caughtException.ShouldNotBeNull();
        var stack = caughtException.GetLogicalStack();

        stack.IsEmpty.ShouldBeFalse();
        stack.Count().ShouldBe(2);

        var frameB = stack.Peek();
        var rest = stack.Pop();
        var frameA = rest.Peek();

        frameB.LineNumber.ShouldBe(frameBLine);
        frameA.LineNumber.ShouldBe(frameALine);
    }
    
    [Test]
    public async Task Should_Preserve_Stack_In_Backpressure_Scenario_With_Throttle() {
        var tcs = new TaskCompletionSource<Exception>();
        int frameALine;
        int frameBLine;

        var stream = Observable.Interval(TimeSpan.FromMilliseconds(1))
            .Take(5)
            
            .Throttle(TimeSpan.FromMilliseconds(50))
            .PushFrame(filePath: "LogicalStackFrameTests.cs", lineNumber: frameALine = GetLineNumber())
            
            .SelectMany(_ => Observable.Throw<long>(new InvalidOperationException("Error after throttle")))
            .PushFrame(filePath: "LogicalStackFrameTests.cs", lineNumber: frameBLine = GetLineNumber());
        

        using var subscription = stream.Subscribe(
            _ => { },
            ex => tcs.SetResult(ex),
            () => { if (!tcs.Task.IsCompleted) tcs.SetException(new InvalidOperationException("Stream completed without error.")); }
        );

        var caughtException = await tcs.Task;

        caughtException.ShouldNotBeNull();
        var stack = caughtException.GetLogicalStack();

        stack.IsEmpty.ShouldBeFalse();
        stack.Count().ShouldBe(2);

        var frameB = stack.Peek();
        var rest = stack.Pop();
        var frameA = rest.Peek();

        frameB.LineNumber.ShouldBe(frameBLine);
        frameA.LineNumber.ShouldBe(frameALine);
    }
    
    [Test]
    public async Task Should_Clear_Stack_After_Explicit_Disposal() {
        var tcs = new TaskCompletionSource<Exception>();
        int frameBLine;

        var streamA = Observable.Never<long>()
            .PushFrame(filePath: "LogicalStackFrameTests.cs", lineNumber: GetLineNumber());

        using (streamA.Subscribe()) { }

        var streamB = Observable.Return(0L)
            .SelectMany(_ => Observable.Throw<long>(new InvalidOperationException("Error B")))
            .PushFrame(filePath: "LogicalStackFrameTests.cs", lineNumber: frameBLine = GetLineNumber());

        using var subscriptionB = streamB.Subscribe(
            _ => { },
            ex => tcs.SetResult(ex)
        );

        var caughtException = await tcs.Task;

        caughtException.ShouldNotBeNull();
        var stack = caughtException.GetLogicalStack();

        stack.IsEmpty.ShouldBeFalse();
        stack.Count().ShouldBe(1, "The stack should only contain Frame B. The presence of Frame A indicates a context leak from the disposed subscription.");
        
        var frameB = stack.Peek();
        frameB.LineNumber.ShouldBe(frameBLine);
    }


    public static int GetLineNumber([System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0) => lineNumber;
        
    }
}