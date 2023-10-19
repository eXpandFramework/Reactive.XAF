using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static string StringFormat(this object s, params object[] args) 
            => string.IsNullOrEmpty($"{s}")?args.Join(""):string.Format($"{s}",args);
    }
}