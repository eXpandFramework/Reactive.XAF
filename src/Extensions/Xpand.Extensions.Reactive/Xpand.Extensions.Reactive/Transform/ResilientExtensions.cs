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
        public static ResilientObservable<T> ToResilient<T>(this IObservable<T> source, [CallerMemberName] string caller = "") 
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
        public ResilientObservable<TResult> Select<TResult>(Func<T, TResult> selector) {
            var source1 = source;
            var caller1 = caller;

            var newSource = Observable.Create<TResult>(observer => {
                var context = new AmbientFaultContext { DefinitionStackTrace = new StackTrace(1, true), CustomContext = [caller1, "Select"] };
                return source1.Subscribe( 
                    onNext: item => {
                        try {
                            observer.OnNext(selector(item));
                        } catch (Exception ex) {
                            new FaultHubException("Error in Select", ex, context).Publish();
                        }
                    },
                    onError: observer.OnError,
                    onCompleted: observer.OnCompleted
                );
            });
            return new ResilientObservable<TResult>(newSource, caller1);
        }
        
        public ResilientObservable<TResult> Cast<TResult>()
            => Select(item => (TResult)(object)item);
        public ResilientObservable<TResult> OfType<TResult>()
            => Where(item => item is TResult).Cast<TResult>();
        public ResilientObservable<T> TakeUntil<TOther>(IObservable<TOther> other) {
            var newSource = source.TakeUntil(other);
            return new ResilientObservable<T>(newSource, caller);
        }
        public ResilientObservable<T> Do(Action<T> action) {
            var source1 = source;
            var caller1 = caller;

            var newSource = Observable.Create<T>(observer => {
                var context = new AmbientFaultContext { DefinitionStackTrace = new StackTrace(1, true), CustomContext = [caller1, "Do"] };
                return source1.Subscribe(
                    onNext: item => {
                        try {
                            action(item);
                        } catch (Exception ex) {
                            new FaultHubException("Error in Do", ex, context).Publish();
                        }
                        observer.OnNext(item);
                    },
                    onError: observer.OnError,
                    onCompleted: observer.OnCompleted
                );
            });
            return new ResilientObservable<T>(newSource, caller1);
        }
        
        public ResilientObservable<T> Take(int count) => new(source.Take(count), caller);

        public ResilientObservable<T> Skip(int count) => new(source.Skip(count), caller);

        public ResilientObservable<T> Where(Func<T, bool> predicate) {
            var source1 = source;
            var caller1 = caller;
    
            var newSource = Observable.Create<T>(observer => {
                var context = new AmbientFaultContext { DefinitionStackTrace = new StackTrace(1, true), CustomContext = [caller1, "Where"] };
                return source1.Subscribe( 
                    onNext: item => {
                        try {
                            if (predicate(item)) {
                                observer.OnNext(item);
                            }
                        } catch (Exception ex) {
                            new FaultHubException("Error in Where", ex, context).Publish();
                        }
                    },
                    onError: observer.OnError,
                    onCompleted: observer.OnCompleted
                );
            });
            return new ResilientObservable<T>(newSource, caller1);
        }
        public ResilientObservable<TResult> SelectMany<TResult>(Func<T, int, IObservable<TResult>> selector) {
            var source1 = source;
            var caller1 = caller;

            var newSource = source1.SelectMany((item, index) => selector(item, index).WithFaultContext(caller: caller1)); 
            return new ResilientObservable<TResult>(newSource, caller1);
        }
        public ResilientObservable<TResult> SelectMany<TResult>(Func<T, IObservable<TResult>> selector) {
            var source1 = source;
            var caller1 = caller;

            var newSource = source1.SelectMany(item => selector(item).WithFaultContext(caller:caller1)); 
            return new ResilientObservable<TResult>(newSource, caller1);
        }
        public IDisposable Subscribe(IObserver<T> observer) => source.Subscribe(observer);
        
        public IObservable<T> AsObservable() => source;
        
        public ResilientObservable<T> StartWith(params T[] values) {
            var newSource = source.StartWith(values);
            return new ResilientObservable<T>(newSource, caller);
        }
    }
}