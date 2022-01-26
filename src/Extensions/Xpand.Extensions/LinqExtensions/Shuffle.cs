using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Xpand.Extensions.LinqExtensions;

public static partial class LinqExtensions {
    static readonly RNGCryptoServiceProvider Provider = new();
    public static IList<T> Shuffle<T>(this IList<T> list) {
        int n = list.Count;
        while (n > 1) {
            byte[] box = new byte[1];
            do Provider.GetBytes(box);
            while (!(box[0] < n * (Byte.MaxValue / n)));
            int k = (box[0] % n);
            n--;
            (list[k], list[n]) = (list[n], list[k]);
        }

        return list;
    }}