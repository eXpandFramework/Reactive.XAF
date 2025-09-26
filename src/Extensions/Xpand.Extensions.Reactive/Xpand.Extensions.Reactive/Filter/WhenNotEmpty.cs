﻿using System;
using System.Collections;
using System.Linq;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Filter{
    public static partial class Filter{
        public static IObservable<TOut> WhenNotEmpty<TOut>(this IObservable<TOut> source) where TOut:IEnumerable
            => source.Where(outs => outs.Cast<object>().Any());
        public static IObservable<TOut> WhenEmpty<TOut>(this IObservable<TOut> source) where TOut:IEnumerable
            => source.Where(outs => !outs.Cast<object>().Any());

    }
}