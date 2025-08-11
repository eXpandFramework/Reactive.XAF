using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub {
    public static class ItemResilientOperators
    {
        public static IObservable<T> DoItemResilient<T>(this IObservable<T> source, Action<T> action,
            object[] context = null, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => source.DoItemResilient(action, null, context, memberName, filePath, lineNumber);
            
        public static IObservable<T> DoItemResilient<T>(this IObservable<T> source, Action<T> action,
            Func<IObservable<T>, IObservable<T>> retryStrategy, object[] context = null, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => source.SelectMany(item => {
                var stream = Observable.Defer(() => {
                    action(item);
                    return Observable.Empty<T>();
                }).PushStackFrame(memberName, filePath, lineNumber);
                var resilientSource = retryStrategy != null ? retryStrategy(stream) : stream;
                return resilientSource
                    .Catch((Exception ex) => {
                        ex.ExceptionToPublish(context.NewFaultContext(memberName)).Publish();
                        return Observable.Empty<T>();
                    })
                    .SafeguardSubscription((ex, _) => ex.ExceptionToPublish(context.NewFaultContext(memberName)).Publish())
                    .StartWith(item);
            });
        
        public static IObservable<T> DeferItemResilient<T>(this object _, Func<IObservable<T>> factory,
            object[] context = null, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => _.DeferItemResilient(factory,null,context,memberName,filePath,lineNumber);
        
        public static IObservable<T> DeferItemResilient<T>(this object _, Func<IObservable<T>> factory, Func<IObservable<T>, IObservable<T>> retryStrategy,
            object[] context = null, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0) {
            var stream = Observable.Defer(() => {
                try {
                    return factory().PushStackFrame(memberName, filePath, lineNumber);
                }
                catch (Exception ex) {
                    return Observable.Throw<T>(ex);
                }
            });
            var resilientSource = retryStrategy != null ? retryStrategy(stream) : stream;
            return resilientSource.Catch((Exception ex) => {
                ex.ExceptionToPublish(context.NewFaultContext(memberName)).Publish();
                return Observable.Empty<T>();
            })
            .SafeguardSubscription((ex, _) => ex.ExceptionToPublish(context.NewFaultContext(memberName)).Publish());
        }
        
        public static IObservable<TResult> SelectItemResilient<TSource, TResult>(this IObservable<TSource> source, Func<TSource, TResult> selector,
            object[] context = null, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => source.SelectItemResilient(selector,null,context,memberName,filePath,lineNumber);
            
        public static IObservable<TResult> SelectItemResilient<TSource, TResult>(this IObservable<TSource> source,
            Func<TSource, TResult> selector, Func<IObservable<TResult>, IObservable<TResult>> retryStrategy,
            object[] context = null, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => source.SelectMany(item => {
                var stream = Observable.Defer(() => Observable.Return(selector(item))).PushStackFrame(memberName, filePath, lineNumber);
                var resilientSource = retryStrategy != null ? retryStrategy(stream) : stream;
                return resilientSource.Catch((Exception ex) => {
                    ex.ExceptionToPublish(new object[] { item }.Concat(context ?? Enumerable.Empty<object>()).ToArray().NewFaultContext(memberName, filePath, lineNumber)).Publish();
                    return Observable.Empty<TResult>();
                })
                .SafeguardSubscription((ex, _) => ex.ExceptionToPublish(new object[] { item }.Concat(context ?? Enumerable.Empty<object>()).ToArray().NewFaultContext(memberName, filePath, lineNumber)).Publish());
            });
            
        public static IObservable<TSource> UsingItemResilient<TSource, TResource>(this object o,Func<TResource> resourceFactory,
            Func<TResource, IObservable<TSource>> observableFactory, object[] context = null,
            [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0) where TResource : IDisposable
            => o.UsingItemResilient(resourceFactory, observableFactory,null,context,memberName, filePath, lineNumber);
        
        public static IObservable<TSource> UsingItemResilient<TSource, TResource>(this object o, Func<TResource> resourceFactory, Func<TResource, IObservable<TSource>> observableFactory,
            Func<IObservable<TSource>, IObservable<TSource>> retryStrategy, object[] context = null, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0) where TResource : IDisposable
            => o.DeferItemResilient(() => Observable.Using(resourceFactory, observableFactory), retryStrategy, context, memberName, filePath, lineNumber);
        
        public static IObservable<TResult> SelectManyItemResilient<TSource, TResult>(this IObservable<TSource> source,
            Func<TSource, int, IObservable<TResult>> resilientSelector, object[] context, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => source.SelectManyItemResilient(resilientSelector,null,context,memberName,filePath,lineNumber);
        
        public static IObservable<TResult> SelectManyItemResilient<TSource, TResult>(this IObservable<TSource> source,
            Func<TSource, IObservable<TResult>> resilientSelector, object[] context=null, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => source.SelectManyItemResilient((sourceItem, _) => resilientSelector(sourceItem), context, memberName,filePath,lineNumber);
        
        public static IObservable<TResult> SelectManyItemResilient<TSource, TResult>(this IObservable<TSource> source,
            Func<TSource, IEnumerable<TResult>> resilientSelector, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => source.SelectManyItemResilient(arg => resilientSelector(arg).ToObservable(), [], memberName,filePath,lineNumber);
        
        public static IObservable<TResult> SelectManySequentialItemResilient<TSource, TResult>(this IObservable<TSource> source,
            Func<TSource, int, IObservable<TResult>> resilientSelector, object[] context, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => source.SelectManySequentialItemResilient(resilientSelector,null, context, memberName,filePath,lineNumber);
        
        // public static IObservable<TResult> SelectManySequentialItemResilient<TSource, TResult>(this IObservable<TSource> source,
        //     Func<TSource, IObservable<TResult>> resilientSelector, object[] context, [CallerMemberName] string caller = "")
        //     => source.SelectManySequentialItemResilient((sourceItem, _) => resilientSelector(sourceItem), context, caller);
        
        public static IObservable<TResult> SelectManyItemResilient<TSource, TResult>(this IObservable<TSource> source,
            Func<TSource, int, IObservable<TResult>> resilientSelector, Func<IObservable<TResult>, IObservable<TResult>> retryStrategy,
            object[] context = null, [CallerMemberName] string caller = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => source.SelectMany((sourceItem, index) => {
                var stream = resilientSelector(sourceItem, index).PushStackFrame(caller, filePath, lineNumber);
                var resilientSource = retryStrategy != null ? retryStrategy(stream) : stream;
                return resilientSource.Catch((Exception ex) => {
                    ex.ExceptionToPublish(new object[] { sourceItem }.Concat(context??Enumerable.Empty<object>()).ToArray().NewFaultContext(caller)).Publish();
                    return Observable.Empty<TResult>();
                });
            });
            
        public static IObservable<TResult> SelectManyItemResilient<TSource, TResult>(this IObservable<TSource> source,
            Func<TSource, IObservable<TResult>> resilientSelector, Func<IObservable<TResult>, IObservable<TResult>> retryStrategy,
            object[] context = null, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => source.SelectManyItemResilient((sourceItem, _) => resilientSelector(sourceItem), retryStrategy, context,memberName, filePath, lineNumber);
        
        public static IObservable<TResult> SelectManySequentialItemResilient<TSource, TResult>(
            this IObservable<TSource> source,
            Func<TSource, int, IObservable<TResult>> resilientSelector,
            Func<IObservable<TResult>, IObservable<TResult>> retryStrategy,
            object[] context = null, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => source.SelectManySequential((sourceItem, index) => {
                var stream = resilientSelector(sourceItem, index).PushStackFrame(memberName, filePath, lineNumber);
                var resilientSource = retryStrategy != null ? retryStrategy(stream) : stream;
                return resilientSource.Catch((Exception ex) => {
                    ex.ExceptionToPublish(new object[] { sourceItem }.Concat(context??Enumerable.Empty<object>()).ToArray()
                        .NewFaultContext(memberName)).Publish();
                    return Observable.Empty<TResult>();
                });
            });
        
        public static IObservable<TResult> SelectManySequentialItemResilient<TSource, TResult>(this IObservable<TSource> source, Func<TSource, IObservable<TResult>> resilientSelector,
            Func<IObservable<TResult>, IObservable<TResult>> retryStrategy=null, object[] context = null, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => source.SelectManySequentialItemResilient((sourceItem, _) => resilientSelector(sourceItem), retryStrategy, context, memberName,filePath,lineNumber);
    }
}
