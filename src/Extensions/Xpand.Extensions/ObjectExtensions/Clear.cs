using System;

namespace Xpand.Extensions.ObjectExtensions {
    public static partial class ObjectExtensions {
        public static void Clear<T>(this T[] array) => array.Clear(0,array.Length);
        public static void Clear<T>(this T[] array,int index) => array.Clear(index,array.Length-index);
        public static void Clear<T>(this T[] array,int index,int length) => Array.Clear(array, index, length);
    }
}