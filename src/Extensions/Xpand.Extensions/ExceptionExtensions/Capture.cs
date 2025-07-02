using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Fasterflect;

namespace Xpand.Extensions.ExceptionExtensions {
    public static partial class ExceptionExtensions {
        public static ExceptionDispatchInfo Capture(this Exception exception)
            => ExceptionDispatchInfo.Capture(exception);
        public static Exception CaptureStack(this Exception exception,int skipFrames=0) {
            exception.SetFieldValue("_stackTraceString",new StackTrace(skipFrames+1,true).ToString());
            return exception;
        }
    }
}