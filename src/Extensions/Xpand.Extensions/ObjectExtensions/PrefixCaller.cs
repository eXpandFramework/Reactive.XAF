using System.Runtime.CompilerServices;

namespace Xpand.Extensions.ObjectExtensions {
    public static partial class ObjectExtensions {
        public static string PrefixCaller(this object value, [CallerMemberName] string caller = "")
            => $"{caller}, {value}";
        public static string SuffixCaller(this object value, [CallerMemberName] string caller = "")
            => $"{value}, {caller}";
    }
}