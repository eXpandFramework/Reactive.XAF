using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub {
    public static class ItemResilientOperators {
        private static IObservable<T> SuppressAndPublishOnFault<T>(this IObservable<T> source,
            object[] context, string memberName, string filePath, int lineNumber)
            => source.Catch((Exception ex) => {
                var faultContext = context.NewFaultContext(memberName, filePath, lineNumber);
                return ex.ProcessFault(
                    faultContext,
                    proceedAction: enrichedException => {
                        enrichedException.Publish();
                        return Observable.Empty<T>();
                    });
            });
        
        static IObservable<T> ApplyItemResilience<T>(this IObservable<T> source, 
            object[] context, string memberName, string filePath, int lineNumber) 
            => source.PushStackFrame(memberName, filePath, lineNumber)
                .SuppressAndPublishOnFault(context, memberName, filePath, lineNumber)
                .SafeguardSubscription((ex, _) => ex.ExceptionToPublish(context.NewFaultContext(memberName, filePath, lineNumber)).Publish());
        
        public static IObservable<T> ProcessEvent<TEventArgs, T>(this object source, string eventName, Func<TEventArgs, IObservable<T>> resilientSelector, 
            object[] context = null, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, 
            IScheduler scheduler = null) where TEventArgs : EventArgs
            => source.FromEventPattern<TEventArgs>(eventName, scheduler)
                .PushStackFrame(memberName, filePath, lineNumber)
                .SelectMany(pattern => resilientSelector(pattern.EventArgs)
                    .SuppressAndPublishOnFault(context.AddToContext(pattern.EventArgs), memberName, filePath, lineNumber));
        
        public static IObservable<T> DoItemResilient<T>(this IObservable<T> source, Action<T> action,
            object[] context = null, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => source.DoItemResilient(action, null, context, memberName, filePath, lineNumber);
            
        public static IObservable<T> DoItemResilient<T>(this IObservable<T> source, Action<T> action,
            Func<IObservable<T>, IObservable<T>> retryStrategy, object[] context = null, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => source
                .PushStackFrame(memberName, filePath, lineNumber)
                .SelectMany(item => {
                    var stream = Observable.Defer(() => {
                        action(item);
                        return Observable.Empty<T>();
                    });
                    var resilientSource = retryStrategy != null ? retryStrategy(stream) : stream;
                    return resilientSource
                        .SuppressAndPublishOnFault(context.AddToContext( item), memberName, filePath, lineNumber)
                        .StartWith(item);
                });

        public static IObservable<T> DeferItemResilient<T>(this object _, Func<IObservable<T>> factory,
            object[] context = null, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => _.DeferItemResilient(factory,null,context,memberName,filePath,lineNumber);
        
        public static IObservable<T> DeferItemResilient<T>(this object _, Func<IObservable<T>> factory, Func<IObservable<T>, IObservable<T>> retryStrategy,
            object[] context = null, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0) {
            var stream = Observable.Defer(() => {
                try {
                    return factory();
                }
                catch (Exception ex) {
                    return Observable.Throw<T>(ex);
                }
            });
            var resilientSource = retryStrategy != null ? retryStrategy(stream) : stream;
            return resilientSource.ApplyItemResilience(context, memberName, filePath, lineNumber);
        }
        
        public static IObservable<TResult> SelectItemResilient<TSource, TResult>(this IObservable<TSource> source, Func<TSource, TResult> selector,
            object[] context = null, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => source.SelectItemResilient(selector,null,context,memberName,filePath,lineNumber);
            
        public static IObservable<TResult> SelectItemResilient<TSource, TResult>(this IObservable<TSource> source,
            Func<TSource, TResult> selector, Func<IObservable<TResult>, IObservable<TResult>> retryStrategy,
            object[] context = null, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => source
                .PushStackFrame(memberName, filePath, lineNumber)
                .SelectMany(item => {
                    var stream = Observable.Defer(() => Observable.Return(selector(item)));
                    var resilientSource = retryStrategy != null ? retryStrategy(stream) : stream;
                    return resilientSource.SuppressAndPublishOnFault(context.AddToContext(item), memberName, filePath, lineNumber);
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
        
        public static IObservable<TResult> SelectManyItemResilient<TSource, TResult>(this IObservable<TSource> source,
            Func<TSource, int, IObservable<TResult>> resilientSelector, Func<IObservable<TResult>, IObservable<TResult>> retryStrategy,
            object[] context = null, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => source
                .PushStackFrame(memberName, filePath, lineNumber)
                .SelectMany((sourceItem, index) => {
                    var stream = resilientSelector(sourceItem, index);
                    var resilientSource = retryStrategy != null ? retryStrategy(stream) : stream;
                    return resilientSource.SuppressAndPublishOnFault(context.AddToContext(sourceItem), memberName, filePath, lineNumber);
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
            => source
                .PushStackFrame(memberName, filePath, lineNumber)
                .SelectManySequential((sourceItem, index) => {
                    var stream = resilientSelector(sourceItem, index);
                    var resilientSource = retryStrategy != null ? retryStrategy(stream) : stream;
                    return resilientSource.SuppressAndPublishOnFault(context.AddToContext(sourceItem), memberName, filePath, lineNumber);
                });
        
        public static IObservable<TResult> SelectManySequentialItemResilient<TSource, TResult>(this IObservable<TSource> source, Func<TSource, IObservable<TResult>> resilientSelector,
            Func<IObservable<TResult>, IObservable<TResult>> retryStrategy=null, object[] context = null, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => source.SelectManySequentialItemResilient((sourceItem, _) => resilientSelector(sourceItem), retryStrategy, context, memberName,filePath,lineNumber);
        
        
        
        public static IObservable<T> ContinueOnFault<T>(this IObservable<T> source, object[] context=null,[CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0) 
            => source.ApplyItemResilience(context, memberName, filePath, lineNumber);

        public static IObservable<T> ContinueOnFault<T>(this IObservable<T> source,
            Func<IObservable<T>, IObservable<T>> retryStrategy, object[] context = null,
            [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => (retryStrategy != null ? retryStrategy(source) : source).ApplyItemResilience(context, memberName, filePath, lineNumber);
    }
}