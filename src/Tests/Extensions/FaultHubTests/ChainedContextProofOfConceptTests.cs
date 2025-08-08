using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;

namespace Xpand.Extensions.Tests.FaultHubTests;
[TestFixture]
public class ChainedContextProofOfConceptTests {
    
    private class ContextFrame {
        public string OperationName { get; init; }
        public StackTrace CapturedStackTrace { get; init; }
        public ContextFrame InnerFrame { get; init; }
    }

    
    private class ChainedContextException(string message, Exception inner, ContextFrame context)
        : Exception(message, inner) {
        public ContextFrame Context { get; } = context;

        public override string ToString() {
            var builder = new StringBuilder();
            builder.AppendLine($"Exception: {GetType().Name}");
            builder.AppendLine($"Message: {Message}");
            builder.AppendLine();

            builder.AppendLine("--- Logical Operation Stack ---");
            var frame = Context;
            var depth = 1;
            while (frame != null) {
                builder.AppendLine($"[Frame {depth++}] Operation: '{frame.OperationName}'");
                builder.AppendLine("  --- Invocation Stack ---");
                builder.AppendLine(frame.CapturedStackTrace.ToString());
                frame = frame.InnerFrame;
            }
            builder.AppendLine("--- End of Logical Operation Stack ---");
            builder.AppendLine();

            if (InnerException == null) return builder.ToString();
            builder.AppendLine("--- Original Exception ---");
            builder.AppendLine(InnerException.ToString());
            builder.AppendLine("--- End of Original Exception ---");
            return builder.ToString();
        }
    }
    
    private static class ExceptionWrapper {
        public static Exception Wrap(Exception ex, [CallerMemberName] string caller = "") {
            var newFrame = new ContextFrame {
                OperationName = caller,
                CapturedStackTrace = new StackTrace(1, true),
                InnerFrame = (ex as ChainedContextException)?.Context
            };

            return new ChainedContextException(
                "An error occurred in a chained context.",
                ex is ChainedContextException cex ? cex.InnerException : ex,
                newFrame);
        }
    }
    

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void DatabaseOperation() {
        try {
            throw new InvalidOperationException("Failed to connect to the database.");
        }
        catch (Exception ex) {
            throw ExceptionWrapper.Wrap(ex);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ProcessPayment() {
        try {
            DatabaseOperation();
        }
        catch (Exception ex) {
            throw ExceptionWrapper.Wrap(ex);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private async Task DatabaseOperationAsync() {
        try {
            await Task.Run(() => throw new InvalidOperationException("Failed on a background thread."));
        }
        catch (Exception ex) {
            throw ExceptionWrapper.Wrap(ex);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private async Task ProcessPaymentAsync() {
        try {
            await DatabaseOperationAsync();
        }
        catch (Exception ex) {
            throw ExceptionWrapper.Wrap(ex);
        }
    }

    [Test]
    public void Chained_Context_Is_Preserved_And_Formatted_Correctly_Synchronously() {
        var caughtException = Should.Throw<ChainedContextException>(ProcessPayment);
        
        var output = caughtException.ToString();
        
        output.ShouldContain($"[Frame 1] Operation: '{nameof(ProcessPayment)}'");
        output.ShouldContain($"at {GetType().FullName}.{nameof(ProcessPayment)}");
        
        output.ShouldContain($"[Frame 2] Operation: '{nameof(DatabaseOperation)}'");
        output.ShouldContain($"at {GetType().FullName}.{nameof(DatabaseOperation)}");
        
        output.ShouldContain("System.InvalidOperationException: Failed to connect to the database.");
    }
    
    [Test]
    public async Task Chained_Context_Is_Preserved_And_Formatted_Correctly_Across_Threads() {
        var caughtException = await Should.ThrowAsync<ChainedContextException>(ProcessPaymentAsync);
        
        var output = caughtException.ToString();
        
        output.ShouldContain($"[Frame 1] Operation: '{nameof(ProcessPaymentAsync)}'");
        output.ShouldContain($"at {GetType().FullName}.{nameof(ProcessPaymentAsync)}");
        
        output.ShouldContain($"[Frame 2] Operation: '{nameof(DatabaseOperationAsync)}'");
        output.ShouldContain($"at {GetType().FullName}.{nameof(DatabaseOperationAsync)}");
        
        output.ShouldContain("--- Original Exception ---");
        output.ShouldContain("System.InvalidOperationException: Failed on a background thread.");
    }
}