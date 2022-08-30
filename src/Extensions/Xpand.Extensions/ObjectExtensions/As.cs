namespace Xpand.Extensions.ObjectExtensions {
    public static partial class ObjectExtensions {
        public static T As<T>(this object obj) 
            => obj is T variable ? variable : default;
    }
}