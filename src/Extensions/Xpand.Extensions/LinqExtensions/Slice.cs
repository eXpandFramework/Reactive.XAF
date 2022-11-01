using System;

namespace Xpand.Extensions.LinqExtensions {
    public static partial class LinqExtensions {
        public static T[] Slice<T>(this T[] source, int index, int length) {
            var slice = new T[length];
            Array.Copy(source, index, slice, 0, length);
            return slice;
        }
        public static T[] Slice<T>(this T[] source, int length) 
            => source.Slice(0, length);
    }
}