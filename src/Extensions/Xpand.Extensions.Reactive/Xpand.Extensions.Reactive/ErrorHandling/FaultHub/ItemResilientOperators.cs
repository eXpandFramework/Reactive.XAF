using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Enumerable = System.Linq.Enumerable;

namespace Xpand.Extensions.Reactive.ErrorHandling.FaultHub {
    
    public static class ItemResilientOperators {
        private static IObservable<T> ApplyItemResilience<T>(this IObservable<T> source, Func<IObservable<T>, IObservable<T>> retryStrategy,
            object[] context, string memberName, string filePath, int lineNumber,Func<Exception, IObservable<bool>> publishWhen=null)
            => FaultHub.Enabled ? (retryStrategy != null ? retryStrategy(source) : source)
                .Catch((Exception e) => e.ProcessFault(e.CreateNewFaultContext(context, memberName, filePath, lineNumber), 
                    proceedAction: enrichedException => (publishWhen?.Invoke(enrichedException) ?? true.Observe())
                        .SelectMany(shouldPublish => {
                            if (shouldPublish) enrichedException.Publish();
                            return Observable.Empty<T>();
                        }))).SafeguardSubscription((ex, s) => ex.ExceptionToPublish(FaultHub.LogicalStackContext.Value.NewFaultContext(context,null, s)).Publish(), memberName) : source;

        private static AmbientFaultContext CreateNewFaultContext(this Exception e,object[] context, string memberName, string filePath, int lineNumber){
            var currentFrame = new LogicalStackFrame(memberName, filePath, lineNumber, context);
            return e is FaultHubException?new[] { currentFrame }.NewFaultContext([],null, memberName, filePath, lineNumber):
                new[] { currentFrame }.Concat(FaultHub.LogicalStackContext.Value ?? Enumerable.Empty<LogicalStackFrame>()).ToList()
                    .NewFaultContext(context,null, memberName, filePath, lineNumber);
        }

        public static IObservable<TEventArgs> ProcessEvent<TEventArgs>(this object source, string eventName,Func<TEventArgs, IObservable<TEventArgs>> resilientSelector, 
            object[] context = null, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, IScheduler scheduler = null) where TEventArgs:EventArgs
            => source.ProcessEvent<TEventArgs,TEventArgs>(eventName, resilientSelector, context, memberName, filePath, lineNumber, scheduler);
        
        public static IObservable<T> ProcessEvent<TEventArgs, T>(this object source, string eventName, Func<TEventArgs, IObservable<T>> resilientSelector, 
            object[] context = null, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, 
            IScheduler scheduler = null) where TEventArgs : EventArgs
            => !FaultHub.Enabled ? source.FromEventPattern<TEventArgs>(eventName, scheduler)
                    .SelectMany(pattern => resilientSelector(pattern.EventArgs))
                : source.FromEventPattern<TEventArgs>(eventName, scheduler)
                    .FaultHubFlowContext()
                    .SelectMany(pattern => resilientSelector(pattern.EventArgs)
                        .ApplyItemResilience(null, context.AddToContext(eventName, pattern.EventArgs), memberName, filePath,
                            lineNumber));

    
        public static IObservable<T> DoItemResilient<T>(this IObservable<T> source, Action<T> action,
            object[] context = null, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => source.DoItemResilient(action, null, context, memberName, filePath, lineNumber);
        
        public static IObservable<T> DoItemResilient<T>(this IObservable<T> source, Action<T> action,
            Func<IObservable<T>, IObservable<T>> retryStrategy, object[] context = null, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => source.SelectMany(item => Observable.Defer(() => {
                            action(item);
                            return Observable.Return(item);
                        })
                        .ApplyItemResilience(retryStrategy, context.AddToContext(item), memberName, filePath, lineNumber)
                        .DefaultIfEmpty(item)
                )
                .SafeguardSubscription((ex, s) => ex.ExceptionToPublish(FaultHub.LogicalStackContext.Value.NewFaultContext(context,null, s)).Publish(), memberName);
        
        public static IObservable<T> DeferItemResilient<T>(this object _, Func<IObservable<T>> factory,
            object[] context = null, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => _.DeferItemResilient(factory,null,context,memberName,filePath,lineNumber);
        

        public static IObservable<Unit> DeferActionItemResilient<T>(this T o, Action execute,
            Func<IObservable<Unit>, IObservable<Unit>> retryStrategy, object[] context = null,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
            => o.DeferItemResilient(() => o.DeferAction(execute), retryStrategy, context, memberName, filePath,
                lineNumber);

        public static IObservable<Unit> DeferActionItemResilient<T>(this T o, Action execute,
            object[] context = null, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
            => o.DeferItemResilient(() => o.DeferAction(execute), null, context, memberName, filePath, lineNumber);
        
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
            return stream.ApplyItemResilience(retryStrategy, context, memberName, filePath, lineNumber);
        }
        
        public static IObservable<TResult> SelectItemResilient<TSource, TResult>(this IObservable<TSource> source, Func<TSource, TResult> selector,
            object[] context = null, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => source.SelectItemResilient(selector,null,context,memberName,filePath,lineNumber);
        
        public static IObservable<TResult> SelectItemResilient<TSource, TResult>(this IObservable<TSource> source,
            Func<TSource, TResult> selector, Func<IObservable<TResult>, IObservable<TResult>> retryStrategy,
            object[] context = null, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => source.SelectMany(item => Observable.Defer(() => Observable.Return(selector(item)))
                    .ApplyItemResilience(retryStrategy, context.AddToContext(item), memberName, filePath, lineNumber))
                .SafeguardSubscription((ex, s) => ex.ExceptionToPublish(FaultHub.LogicalStackContext.Value.NewFaultContext(context,null, s)).Publish(), memberName);
            
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
            => source.SelectMany((sourceItem, index) => resilientSelector(sourceItem, index).ApplyItemResilience(retryStrategy, context.AddToContext(sourceItem), memberName, filePath, lineNumber))
                .SafeguardSubscription((ex, s) => ex.ExceptionToPublish(FaultHub.LogicalStackContext.Value.NewFaultContext(context,null, s)).Publish(), memberName);
            
        public static IObservable<TResult> SelectManyItemResilient<TSource, TResult>(this IObservable<TSource> source,
            Func<TSource, IObservable<TResult>> resilientSelector, Func<IObservable<TResult>, IObservable<TResult>> retryStrategy,
            object[] context = null, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => source.SelectManyItemResilient((sourceItem, _) => resilientSelector(sourceItem), retryStrategy, context,memberName, filePath, lineNumber);
        
        public static IObservable<TResult> SelectManySequentialItemResilient<TSource, TResult>(this IObservable<TSource> source,
            Func<TSource, int, IObservable<TResult>> resilientSelector, Func<IObservable<TResult>, IObservable<TResult>> retryStrategy,
            object[] context = null, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => source.SelectManySequential((sourceItem, index) => resilientSelector(sourceItem, index)
                .ApplyItemResilience(retryStrategy, context.AddToContext(sourceItem), memberName, filePath, lineNumber));
        
        public static IObservable<TResult> SelectManySequentialItemResilient<TSource, TResult>(this IObservable<TSource> source, Func<TSource, IObservable<TResult>> resilientSelector,
            Func<IObservable<TResult>, IObservable<TResult>> retryStrategy=null, object[] context = null, [CallerMemberName]string memberName="",[CallerFilePath]string filePath="",[CallerLineNumber]int lineNumber=0)
            => source.SelectManySequentialItemResilient((sourceItem, _) => resilientSelector(sourceItem), retryStrategy, context, memberName,filePath,lineNumber);
        
        
        public static IObservable<T> ContinueOnFault<T>(this IObservable<T> source, Func<IObservable<T>, IObservable<T>> retryStrategy = null,
            object[] context = null,Func<Exception, IObservable<bool>> publishWhen = null, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => source.ApplyItemResilience(retryStrategy,  context, memberName, filePath, lineNumber,publishWhen);
    }
}
