namespace Xpand.Extensions.ObjectExtensions {
    public static partial class ObjectExtensions {
        public static T Cast<T>(this object obj)
            => (T)obj;
    }
}