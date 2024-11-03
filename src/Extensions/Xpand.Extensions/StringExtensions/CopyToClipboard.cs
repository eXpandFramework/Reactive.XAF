using TextCopy;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static void CopyToClipboard(this string s) => new Clipboard().SetText(s);
    }
}