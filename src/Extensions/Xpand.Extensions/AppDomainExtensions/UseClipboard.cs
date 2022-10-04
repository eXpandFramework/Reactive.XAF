using System;
using System.Windows;

namespace Xpand.Extensions.AppDomainExtensions {
    public static partial class AppDomainExtensions {
        public static void UseClipboard(this AppDomain domain, Action action) {
            var currentText = Clipboard.GetText();
            Clipboard.SetText("");
            action();
            Clipboard.SetText(currentText);
        }
    }
}