namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static string StringFormat(this object s, params object[] args)
            => string.Format($"{s}",args);
    }
}