using System;
using System.Linq;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static bool IsWhiteSpace(this string value) => value.All(Char.IsWhiteSpace);
    }
}