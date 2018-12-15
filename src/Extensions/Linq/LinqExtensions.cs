using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DevExpress.XAF.Extensions.Linq{
    public static class LinqExtensions{
        public static IEnumerable<T> TakeAllButLast<T>(this IEnumerable<T> source) {
            using (var it = source.GetEnumerator()){
                bool hasRemainingItems;
                bool isFirst = true;
                T item = default;
                do {
                    hasRemainingItems = it.MoveNext();
                    if (hasRemainingItems) {
                        if (!isFirst) yield return item;
                        item = it.Current;
                        isFirst = false;
                    }
                } while (hasRemainingItems);
            }
        }

        public static IEnumerable<T> GetItems<T>(this IEnumerable collection,
            Func<T, IEnumerable> selector) {
            var stack = new Stack<IEnumerable<T>>();
            stack.Push(collection.OfType<T>());

            while (stack.Count > 0) {
                IEnumerable<T> items = stack.Pop();
                foreach (var item in items) {
                    yield return item;

                    IEnumerable<T> children = selector(item).OfType<T>();
                    stack.Push(children);
                }
            }
        }

        public static int FindIndex<T>(this IList<T> list, Predicate<T> predicate) {
            for(int i = 0; i < list.Count; i++)
                if(predicate(list[i]))
                    return i;
            return -1;
        }

        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> list, int parts) {
            int i = 0;
            return list.GroupBy(item => i++%parts).Select(part => part.AsEnumerable());
        }

        public static IEnumerable<T> SkipLastN<T>(this IEnumerable<T> source, int n) {
            using (var it = source.GetEnumerator()){
                bool hasRemainingItems;
                var cache = new Queue<T>(n + 1);

                do{
                    var b = hasRemainingItems = it.MoveNext();
                    if (b) {
                        cache.Enqueue(it.Current);
                        if (cache.Count > n)
                            yield return cache.Dequeue();
                    }
                } while (hasRemainingItems);
            }
        }
        public static IEnumerable<TResult> SelectNonNull<T, TResult>(this IEnumerable<T> sequence,
            Func<T, TResult> projection) where TResult : class{
            return sequence.Select(projection).Where(e => e != null);
        }

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> sequence) where T : class{
            return sequence.Where(e => e != null);
        }

        public static TResult NullSafeEval<TSource, TResult>(this TSource source,
            Expression<Func<TSource, TResult>> expression, TResult defaultValue){
            Expression<Func<TSource, TResult>> safeExp = Expression.Lambda<Func<TSource, TResult>>(
                NullSafeEvalWrapper(expression.Body, Expression.Constant(defaultValue)),
                expression.Parameters[0]);

            Func<TSource, TResult> safeDelegate = safeExp.Compile();
            return safeDelegate(source);
        }

        private static Expression NullSafeEvalWrapper(Expression expr, Expression defaultValue){
            Expression safe = expr;

            while (!IsNullSafe(expr, out var obj)){
                BinaryExpression isNull = Expression.Equal(obj, Expression.Constant(null));

                safe =
                    Expression.Condition
                    (
                        isNull,
                        defaultValue,
                        safe
                    );

                expr = obj;
            }
            return safe;
        }

        private static bool IsNullSafe(Expression expr, out Expression nullableObject){
            nullableObject = null;

            if (expr is MemberExpression || expr is MethodCallExpression){
                Expression obj;
                var memberExpr = expr as MemberExpression;
                var callExpr = expr as MethodCallExpression;

                if (memberExpr != null){
                    // Static fields don't require an instance
                    var field = memberExpr.Member as FieldInfo;
                    if (field != null && field.IsStatic)
                        return true;

                    // Static properties don't require an instance
                    var property = memberExpr.Member as PropertyInfo;
                    MethodInfo getter = property?.GetGetMethod();
                    if (getter != null && getter.IsStatic)
                        return true;
                    obj = memberExpr.Expression;
                }
                else{
                    // Static methods don't require an instance
                    if (callExpr.Method.IsStatic)
                        return true;

                    obj = callExpr.Object;
                }

                // Value types can't be null
                if (obj != null && obj.Type.IsValueType)
                    return true;

                // Instance member access or instance method call is not safe
                nullableObject = obj;
                return false;
            }
            return true;
        }


        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> sequence)
            where T : struct{
            return sequence.Where(e => e != null).Select(e => e.Value);
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector){
            var seenKeys = new HashSet<TKey>();
            return source.Where(element => seenKeys.Add(keySelector(element)));
        }
    }
}