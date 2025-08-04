using System;
using System.Threading.Tasks;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.TypeExtensions;

namespace Xpand.Extensions.ObjectExtensions {

    public static partial class ObjectExtensions {
        public static string EnsureString(this object o)
            => o?.ToString().EnsureEndWith(String.Empty)??String.Empty;

        public static string EnsureStringEndWith(this object o, string end)
            => o?.ToString().EnsureEndWith(end)??end;
        public static T As<T>(this object obj) 
            => obj is T variable ? variable : default;
        public static T As<T>(this T obj,string typeName) {
            var type = obj?.GetType();
            return type == null ? default : type.IsInterface ? type.Implements(typeName) ? obj : default :
                type.InheritsFrom(typeName) ? obj : default;
        }
    }
}