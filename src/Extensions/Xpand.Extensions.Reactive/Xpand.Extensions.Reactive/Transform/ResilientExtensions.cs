using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.Reactive.ErrorHandling;

namespace Xpand.Extensions.Reactive.Transform{
    public static class ResilientExtensions {
        public static ResilientObservable<TEventArgs> ProcessEvent<TEventArgs>(this object source,
            string eventName, IScheduler scheduler = null, [CallerMemberName] string caller = "") {
            
            var initialObservable = source.FromEventPattern<TEventArgs>(eventName, scheduler, caller)
                .Select(pattern => pattern.EventArgs);
            
            return new ResilientObservable<TEventArgs>(initialObservable, caller);
        }        
        public static ResilientObservable<T> ToResilientObservable<T>(this IObservable<T> source, [CallerMemberName] string caller = "") 
            => new(source, caller);
        public static ResilientObservable<EventPattern<object>> ProcessEvent(this object source, string eventName, IScheduler scheduler = null, [CallerMemberName] string caller = "") {
            var eventStream = source.FromEventPattern<EventArgs>(eventName, scheduler, caller)
                .Select(pattern => new EventPattern<object>(pattern.Sender, pattern.EventArgs));
            return new ResilientObservable<EventPattern<object>>(eventStream, caller);
        }
        public static ResilientObservable<T> Switch<T>(this ResilientObservable<IObservable<T>> source, [CallerMemberName] string caller = "") {
            var sourceOfStreams = (IObservable<IObservable<T>>)source;
            var resilientSourceOfStreams = sourceOfStreams.Select(innerStream => 
                innerStream.WithFaultContext(caller: caller));
            return new ResilientObservable<T>(resilientSourceOfStreams.Switch(), caller);
        }
        public static IObservable<bool> Any<TSource>(this ResilientObservable<TSource> source) 
            => ((IObservable<TSource>)source).Any();
        
        public static IObservable<bool> Any<TSource>(this ResilientObservable<TSource> source, Func<TSource, bool> predicate)
            => source.Where(predicate).Any();
        
        public static ResilientObservable<T> Concat<T>(this ResilientObservable<T> source, params IObservable<T>[] others) {
            var allSources = Enumerable.Concat([source], others);
            var resilientSources = allSources.Select(s => s.WithFaultContext());
            return new ResilientObservable<T>(resilientSources.Concat());
        }
        
    }
    public readonly struct ResilientObservable<T>(IObservable<T> source, [CallerMemberName] string caller = "")
        : IObservable<T> {
        
        public ResilientObservable<TResult> Cast<TResult>()
            => Select(item => (TResult)(object)item);
        public ResilientObservable<TResult> OfType<TResult>()
            => Where(item => item is TResult).Cast<TResult>();
        public ResilientObservable<T> TakeUntil<TOther>(IObservable<TOther> other) {
            var newSource = source.TakeUntil(other);
            return new ResilientObservable<T>(newSource, caller);
        }
        public ResilientObservable<T> Do(Action<T> action) {
            var caller1 = caller;
            return source.Select(item => {
                try {
                    action(item);
                }
                catch (Exception ex) {
                    HandleException<T>(ex, caller1);
                }
                return item;
            }).ToResilientObservable(caller1);
        }
        
        public ResilientObservable<T> Take(int count) => new(source.Take(count), caller);

        public ResilientObservable<T> Skip(int count) => new(source.Skip(count), caller);

        public ResilientObservable<TResult> Select<TResult>(Func<T, TResult> selector) {
            var caller1 = caller;
            return source.SelectMany(item => {
                try {
                    return Observable.Return(selector(item));
                }
                catch (Exception ex) {
                    return HandleException<TResult>(ex,caller1);
                }
            }).ToResilientObservable(caller1);
        }

        public ResilientObservable<T> Where(Func<T, bool> predicate) {
            var caller1 = caller;
            return source.SelectMany(item => {
                try {
                    return predicate(item) ? Observable.Return(item) : Observable.Empty<T>();
                }
                catch (Exception ex) {
                    return HandleException<T>(ex,caller1);
                }
            }).ToResilientObservable(caller1);
        }

        private static IObservable<TResult> HandleException<TResult>(Exception ex,string caller,[CallerMemberName]string op=""){
            new FaultHubException($"Error in {op}", ex,  new AmbientFaultContext { DefinitionStackTrace = new StackTrace(2, true), CustomContext = [caller, op] }).Publish();
            return Observable.Empty<TResult>();
        }

        public ResilientObservable<TResult> SelectMany<TResult>(Func<T, int, IObservable<TResult>> selector) {
            var source1 = source;
            var caller1 = caller;
            return source1.SelectMany((item, index) => selector(item, index).WithFaultContext(caller: caller1)).ToResilientObservable(caller1);
        }
        public ResilientObservable<TResult> SelectMany<TResult>(Func<T, IObservable<TResult>> selector) {
            var source1 = source;
            var caller1 = caller;
            return source1.SelectMany(item => selector(item).WithFaultContext(caller:caller1)).ToResilientObservable(caller1);
        }
        public IDisposable Subscribe(IObserver<T> observer) => source.Subscribe(observer);
        
        public IObservable<T> AsObservable() => source;
        
        public ResilientObservable<T> StartWith(params T[] values) {
            var newSource = source.StartWith(values);
            return new ResilientObservable<T>(newSource, caller);
        }

        public bool IsValid => source != null;
        public ResilientObservable<T> Aggregate(Func<T, T, T> accumulator) {
            var newSource = source.Aggregate(accumulator);
            return new ResilientObservable<T>(newSource, caller);
        }

        public ResilientObservable<TAccumulate> Aggregate<TAccumulate>(TAccumulate seed, Func<TAccumulate, T, TAccumulate> accumulator) {
            var newSource = source.Aggregate(seed, accumulator);
            return new ResilientObservable<TAccumulate>(newSource, caller);
        }
    }
}