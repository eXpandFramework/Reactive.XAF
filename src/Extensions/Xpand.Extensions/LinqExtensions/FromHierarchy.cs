using System;
using System.Collections.Generic;
using System.Linq;

namespace Xpand.Extensions.LinqExtensions{
    public enum CycleDetectionStrategy {
        Ignore,
        Throw
    }
    public static partial class LinqExtensions{
        public static IEnumerable<TSource> FromHierarchy<TSource>(this TSource source, Func<TSource, TSource> nextItem) where TSource : class 
            => source.FromHierarchy( nextItem, s => s != null);
        public static IEnumerable<TSource> FromHierarchy<TSource>(this TSource source, Func<TSource, IEnumerable<TSource>> nextItems) where TSource : class
            => source.FromHierarchy(nextItems, s => s != null);

        public static IEnumerable<TSource> FromHierarchy<TSource>(this TSource source, Func<TSource, IEnumerable<TSource>> nextItems, Func<TSource, bool> canContinue)
            => source == null ? [] : new[] { source }.Concat(canContinue(source) ? nextItems(source).SelectMany(child => child.FromHierarchy(nextItems, canContinue)) : []);

        public static IEnumerable<TSource> FromHierarchy<TSource>(this TSource source, Func<TSource, TSource> nextItem, Func<TSource, bool> canContinue){
            for (var current = source; canContinue(current); current = nextItem(current)) yield return current;
        }
        
        public static IEnumerable<IReadOnlyList<TSource>> FromHierarchyAll<TSource>(this TSource leaf,
            Func<TSource, IEnumerable<TSource>> parentSelector, Func<TSource, bool> isRoot = null,CycleDetectionStrategy cycleDetectionStrategy=CycleDetectionStrategy.Throw) {
            isRoot ??= (node => !parentSelector(node).Any());
            var currentPath = new HashSet<TSource>();
            IEnumerable<IReadOnlyList<TSource>> Recurse(TSource node, Stack<TSource> stack) {
                if (!currentPath.Add(node)) {
                    if (cycleDetectionStrategy == CycleDetectionStrategy.Throw) {
                        throw new ExceptionExtensions.ExceptionExtensions.CircularDependencyException($"Circular dependency detected at node: {node}");
                    }

                    yield break;
                }
        
                stack.Push(node);
        
                if (isRoot(node)) {
                    yield return stack.Reverse().ToList();
                }
                else {
                    foreach (var parent in parentSelector(node)) {
                        foreach (var path in Recurse(parent, stack)) {
                            yield return path;
                        }
                    }
                }
        
                stack.Pop();
                currentPath.Remove(node);
            }

            return Recurse(leaf, new Stack<TSource>());
        }
    }
}