namespace Xpand.Extensions.ObjectExtensions {
    public static partial class ObjectExtensions {
        public static T To<T>(this object obj)
            => (T)obj;
    }
}