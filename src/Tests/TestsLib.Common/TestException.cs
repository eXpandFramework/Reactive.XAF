using System;
using System.Runtime.CompilerServices;
using Xpand.Extensions.ExceptionExtensions;

namespace Xpand.TestsLib.Common{
    public class TestTimeoutException:Exception {
        
    }
    public sealed class TestException : Exception{
        private TestException(Exception exception,[CallerMemberName]string caller="") : this(
            $"{exception.GetType().Name} - {caller} - {exception.Message}{Environment.NewLine}{Environment.NewLine}{ScreenCapture.CaptureActiveWindowAndSave()}.{Environment.NewLine}",
            $"{Environment.NewLine}{exception.ReverseStackTrace()}")
            => ExceptionType = exception.GetType();

        public Type ExceptionType { get; private set; }

        public static TestException New(Exception exception,[CallerMemberName]string caller="") => new(exception,caller);

        private TestException(string message, string stackTrace) : base(message) => Data["StackTrace"] = stackTrace;

        public override string StackTrace => Data["StackTrace"] as string;
    }
}