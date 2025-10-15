﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HarmonyLib;

namespace Xpand.Extensions.LinqExtensions{
    public static partial class LinqExtensions{
        public static IEnumerable<TValue> To<TSource,TValue>(this IEnumerable<TSource> source,TValue value) 
            => source.Select(_ => value);

        public static IEnumerable<T> To<T>(this IEnumerable<object> source,bool newInstance=false) 
            => source.Select(_ =>!newInstance? default:(T)typeof(T).CreateInstance()).WhereNotDefault();

        public static IEnumerable<T> To<TResult,T>(this IEnumerable<TResult> source) 
            => source.Select(_ => default(T)).WhereNotDefault();
        
        public static ImmutableStack<T> ToImmutableStack<T>(this IEnumerable<T> source) => ImmutableStack.Create(source.ToArray());
        
    }
}