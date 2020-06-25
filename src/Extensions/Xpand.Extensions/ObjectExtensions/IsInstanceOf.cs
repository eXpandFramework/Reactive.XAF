using Xpand.Extensions.TypeExtensions;

namespace Xpand.Extensions.ObjectExtensions{
    public static partial class ObjectExtensions{
        public static bool IsInstanceOf<TSource>(this TSource source, string typeName) where TSource : class{
            return source != null && source.GetType().InheritsFrom(typeName);
        }
    }
}