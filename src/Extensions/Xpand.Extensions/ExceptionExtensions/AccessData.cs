using System;
using System.Collections;

namespace Xpand.Extensions.ExceptionExtensions {
    public static partial class ExceptionExtensions {
        public static T AccessData<T>(this Exception exception, Func<IDictionary, T> accessor)   {
            if (exception == null) return default;
            lock (exception.Data.SyncRoot) {
                return accessor(exception.Data);
            }
        }

        public static void AccessData(this Exception exception, Action<IDictionary> accessor)  {
            if (exception == null) return ;
            lock (exception.Data.SyncRoot) {
                accessor(exception.Data);
            }
        }
    }
}