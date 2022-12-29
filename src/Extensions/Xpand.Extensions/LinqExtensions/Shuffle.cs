using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Xpand.Extensions.LinqExtensions {
    public static partial class LinqExtensions {
        
        public static IList<T> Shuffle<T>(this IList<T> list) {
            using var generator = RandomNumberGenerator.Create();
            var n = list.Count;
            while (n > 1) {
                var box = new byte[1];
                do generator.GetBytes(box);
                while (!(box[0] < n * (Byte.MaxValue / n)));
                var k = (box[0] % n);
                n--;
                (list[k], list[n]) = (list[n], list[k]);
            }

            return list;
        }}
}