using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub {
    public static class ItemResilientOperators {
        
        // public static IObservable<T> DoItemResilient<T>(this IObservable<T> source, Action<T> action, object[] context = null, [CallerMemberName] string caller = "")
        //     => source.DoItemResilient(action,null,context,caller);
            
        public static IObservable<T> DoItemResilient<T>(this IObservable<T> source, Action<T> action,
            Func<IObservable<T>, IObservable<T>> retryStrategy = null, object[] context = null, [CallerMemberName] string caller = "")
            => source.SelectMany(item => Observable.Defer(() => {
                    action(item);
                    return Observable.Empty<T>();
                })
                .ApplyItemResilience(retryStrategy, context, caller)
                .StartWith(item)
            );
        
        public static IObservable<T> DeferItemResilient<T>(this object _, Func<IObservable<T>> factory,
            object[] context = null, [CallerMemberName] string caller = "")
            => _.DeferItemResilient(factory,null,context,caller);
        
        public static IObservable<T> DeferItemResilient<T>(this object _, Func<IObservable<T>> factory, Func<IObservable<T>, IObservable<T>> retryStrategy = null,
            object[] context = null, [CallerMemberName] string caller = "")
            => Observable.Defer(() => {
                    try {
                        return factory();
                    }
                    catch (Exception ex) {
                        return Observable.Throw<T>(ex);
                    }
                })
                .ApplyItemResilience(retryStrategy, context, caller);
        // public static IObservable<TResult> SelectItemResilient<TSource, TResult>(this IObservable<TSource> source, Func<TSource, TResult> selector,
        //     object[] context = null, [CallerMemberName] string caller = "")
        //     => source.SelectItemResilient(selector,null,context,caller);

        public static IObservable<TResult> SelectItemResilient<TSource, TResult>(this IObservable<TSource> source,
            Func<TSource, TResult> selector, Func<IObservable<TResult>, IObservable<TResult>> retryStrategy = null,
            object[] context = null, [CallerMemberName] string caller = "")
            => source.SelectMany(item => Observable.Defer(() => Observable.Return(selector(item)))
                    .ApplyItemResilience(retryStrategy, context, caller, item)
            );
        public static IObservable<TSource> UsingItemResilient<TSource, TResource>(this object o,Func<TResource> resourceFactory,
            Func<TResource, IObservable<TSource>> observableFactory, object[] context = null,
            [CallerMemberName] string caller = "") where TResource : IDisposable
            => o.UsingItemResilient(resourceFactory, observableFactory,null,context,caller);
        
        public static IObservable<TSource> UsingItemResilient<TSource, TResource>(this object o, Func<TResource> resourceFactory, Func<TResource, IObservable<TSource>> observableFactory,
            Func<IObservable<TSource>, IObservable<TSource>> retryStrategy = null, object[] context = null, [CallerMemberName] string caller = "") where TResource : IDisposable
            => o.DeferItemResilient(() => Observable.Using(resourceFactory, observableFactory), retryStrategy, context, caller);
        public static IObservable<TResult> SelectManyItemResilient<TSource, TResult>(this IObservable<TSource> source,
            Func<TSource, int, IObservable<TResult>> resilientSelector, object[] context, [CallerMemberName] string caller = "")
            => source.SelectManyItemResilient(resilientSelector,null,context,caller);
        
        public static IObservable<TResult> SelectManyItemResilient<TSource, TResult>(this IObservable<TSource> source,
            Func<TSource, IObservable<TResult>> resilientSelector, object[] context, [CallerMemberName] string caller = "")
            => source.SelectManyItemResilient((sourceItem, _) => resilientSelector(sourceItem), context, caller);
        
        // public static IObservable<TResult> SelectManyItemResilient<TSource, TResult>(this IObservable<TSource> source,
        //     Func<TSource, IObservable<TResult>> resilientSelector, [CallerMemberName] string caller = "")
        //     => source.SelectManyItemResilient(resilientSelector, [], caller);
        
        public static IObservable<TResult> SelectManyItemResilient<TSource, TResult>(this IObservable<TSource> source,
            Func<TSource, IEnumerable<TResult>> resilientSelector, [CallerMemberName] string caller = "")
            => source.SelectManyItemResilient(arg => resilientSelector(arg).ToObservable(), [], caller);
        
        public static IObservable<TResult> SelectManySequentialItemResilient<TSource, TResult>(this IObservable<TSource> source,
            Func<TSource, int, IObservable<TResult>> resilientSelector, object[] context, [CallerMemberName] string caller = "")
            => source.SelectManySequentialItemResilient(resilientSelector,null, context, caller);
        
        public static IObservable<TResult> SelectManySequentialItemResilient<TSource, TResult>(this IObservable<TSource> source,
            Func<TSource, IObservable<TResult>> resilientSelector, object[] context, [CallerMemberName] string caller = "")
            => source.SelectManySequentialItemResilient((sourceItem, _) => resilientSelector(sourceItem), context, caller);
        
        // public static IObservable<TResult> SelectManySequentialItemResilient<TSource, TResult>(this IObservable<TSource> source,
        //     Func<TSource, IObservable<TResult>> resilientSelector, [CallerMemberName] string caller = "")
        //     => source.SelectManySequentialItemResilient(resilientSelector, [], caller);

        public static IObservable<TResult> SelectManyItemResilient<TSource, TResult>(
            this IObservable<TSource> source,
            Func<TSource, int, IObservable<TResult>> resilientSelector,
            Func<IObservable<TResult>, IObservable<TResult>> retryStrategy = null,
            object[] context = null, [CallerMemberName] string caller = "")
            => source.SelectMany((sourceItem, index) => 
                resilientSelector(sourceItem, index).ApplyItemResilience(retryStrategy, context, caller, sourceItem));

        public static IObservable<TResult> SelectManyItemResilient<TSource, TResult>(
            this IObservable<TSource> source,
            Func<TSource, IObservable<TResult>> resilientSelector,
            Func<IObservable<TResult>, IObservable<TResult>> retryStrategy = null,
            object[] context = null, [CallerMemberName] string caller = "")
            => source.SelectManyItemResilient((sourceItem, _) => resilientSelector(sourceItem), retryStrategy, context, caller);

        public static IObservable<TResult> SelectManySequentialItemResilient<TSource, TResult>(
            this IObservable<TSource> source,
            Func<TSource, int, IObservable<TResult>> resilientSelector,
            Func<IObservable<TResult>, IObservable<TResult>> retryStrategy = null,
            object[] context = null, [CallerMemberName] string caller = "")
            => source.SelectManySequential((sourceItem, index) => 
                resilientSelector(sourceItem, index).ApplyItemResilience(retryStrategy, context, caller, sourceItem));
        
        public static IObservable<TResult> SelectManySequentialItemResilient<TSource, TResult>(this IObservable<TSource> source, Func<TSource, IObservable<TResult>> resilientSelector,
            Func<IObservable<TResult>, IObservable<TResult>> retryStrategy = null, object[] context = null, [CallerMemberName] string caller = "")
            => source.SelectManySequentialItemResilient((sourceItem, _) => resilientSelector(sourceItem), retryStrategy, context, caller);
        
        public static IObservable<T> ApplyItemResilience<T>(this IObservable<T> source,
            Func<IObservable<T>, IObservable<T>> retryStrategy, object[] context, string caller, object lazyContextItem = null) {
            var resilientSource = retryStrategy != null ? retryStrategy(source) : source;
            return resilientSource
                .Catch((Exception ex) => {
                    var finalContext = lazyContextItem != null ? (context ?? []).Concat([lazyContextItem]).ToArray() : context;
                    var faultContext = finalContext.NewFaultContext(caller);
                    ex.ExceptionToPublish(faultContext).Publish();
                    return Observable.Empty<T>();
                })
                .SafeguardSubscription((ex, _) => {
                    var finalContext = lazyContextItem != null ? (context ?? []).Concat([lazyContextItem]).ToArray() : context;
                    ex.ExceptionToPublish(finalContext.NewFaultContext(caller)).Publish();
                });
        }
    }
}