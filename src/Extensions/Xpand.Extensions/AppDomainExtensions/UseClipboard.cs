using System;

namespace Xpand.Extensions.AppDomainExtensions {
    public static partial class AppDomainExtensions {
        public static void UseClipboard(this AppDomain domain, Action action) {
            var currentText = TextCopy.ClipboardService.GetText();
            TextCopy.ClipboardService.SetText("");
            action();
            TextCopy.ClipboardService.SetText($"{currentText}");
        }
    }
}