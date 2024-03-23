using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xpand.Extensions.ExpressionExtensions;

namespace Xpand.Extensions.QueryableExtensions {
    public static class QueryableExtensions {
        public static IEnumerable<T> InRange<T, TValue>(this IQueryable<T> source, Expression<Func<T, TValue>> selector, int blockSize, IEnumerable<TValue> values)
            => values.GetBlocks(blockSize).SelectMany(block => {
                var row = typeof(T).ParameterExpression("row");
                return source.Where(typeof(TValue).ContainsMethod()
                    .MethodCallExpression(block.ConstantExpression(typeof(TValue[])), selector.Invoke(row))
                    .Lambda<Func<T, bool>>(row));
            });

        private static MethodInfo ContainsMethod(this Type type)
            => typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(tmp => tmp.Name == "Contains" && tmp.IsGenericMethodDefinition && tmp.GetParameters().Length == 2)
                .Select(tmp => tmp.MakeGenericMethod(type)).FirstOrDefault();

        public static IEnumerable<T[]> GetBlocks<T>(
            this IEnumerable<T> source, int blockSize) {
            List<T> list = new List<T>(blockSize);
            foreach(T item in source) {
                list.Add(item);
                if(list.Count == blockSize) {
                    yield return list.ToArray();
                    list.Clear();
                }
            }
            if(list.Count > 0) {
                yield return list.ToArray();
            }
        }
    }
    }
