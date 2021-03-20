using System;

namespace Xpand.Extensions.ObjectExtensions {
    public static partial class ObjectExtensions {
        public static T Do<T>(this T obj, Action action,Action<Exception> error=null) {
            try {
                action();
            }
            catch (Exception e){
                error?.Invoke(e);
            }

            return obj;
        }
    }
}