using System;
using Xpand.Extensions.Windows;

namespace Xpand.Extensions.ExceptionExtensions {
    public static partial class ExceptionExtensions {
        public static Exception AddScreenshot(this Exception exception)
            => new($"{exception.Message} {ScreenCapture.CaptureActiveWindowAndSave()}", exception);
    }
}