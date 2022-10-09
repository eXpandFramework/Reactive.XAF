using System;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Filter;

namespace Xpand.Extensions.Reactive.Transform {
    public static partial class Transform {
        public static IObservable<T> When<T>(this object source)
            => source.As<T>().ReturnObservable().WhenNotDefault();
        public static IObservable<T> When<T>(this T source,string typeName)
            => source.As(typeName).ReturnObservable().WhenNotDefault();
    }
}