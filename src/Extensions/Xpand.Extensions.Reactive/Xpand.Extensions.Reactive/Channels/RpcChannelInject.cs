using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;

namespace Xpand.Extensions.Reactive.Channels{
    public static class RpcChannelInject {
        public static IObservable<TSource> Inject<TSource, TSignal>(this IObservable<TSource> source, Func<TSource, TSignal> signalFactory) 
            => source.SelectMany(item => {
                var signal = signalFactory(item);
                return typeof(TSignal).MakeRequest()
                    .TryWith(signal, defaultValue: item)
                    .Do(result => LogFast($"Injection result for {typeof(TSignal).Name}: {result}"));
            });
        
        public static IObservable<Unit> Inject<T, TKey>(this TKey key, Func<T, IObservable<T>> selector) where TKey : notnull
            => key.HandleRequest().With<T, IObservable<T>>(item => Observable.Return(selector(item)));

        public static IObservable<T> InjectEmission<T>(this T source) => Observable.Return(source).Inject(typeof(T));
        public static IObservable<T> Inject<T>(this IObservable<T> source) => source.Inject(typeof(T));
        public static IObservable<T> Inject<T, TKey>(this IObservable<T> source, TKey key) where TKey : notnull
            => source.SelectMany(item => key.MakeRequest()
                .TryWith(item, defaultValue: Observable.Return(item))
                .SelectMany(injectedStream => injectedStream));

        public static IObservable<Unit> InjectWithContext<TContext, TResult, TKey>(this TKey key, Func<TContext, IObservable<TResult>> injectionFactory) where TKey : notnull
            => key.HandleRequest().With<TContext, IObservable<TResult>>(context => Observable.Return(injectionFactory(context)));

        public static IObservable<T> InjectWithContext<T, TContext>(this IObservable<T> source, TContext context, 
            [CallerMemberName] string member = "", [CallerFilePath] string path = "") {
            var scope = Path.GetFileNameWithoutExtension(path);
            var uniqueKey = $"{scope}.{member}";
            LogFast($"Generated Injection Key: '{uniqueKey}' from Path: '{path}'");
            return uniqueKey.MakeRequest()
                .TryWith<TContext, IObservable<T>>(context, defaultValue: null)
                .SelectMany(injected => injected ?? source);
        }
        public static UniversalInjectionBuilder<TKey> Inject<TKey>(this TKey key) where TKey : notnull => new(key);
        public static UniversalInjectionBuilder<string> Inject(this string methodName, Type scope) => new($"{scope.Name}.{methodName}");

        public static UniversalInjectionBuilder<string> Inject(this string methodName, string scopeName) => new($"{scopeName}.{methodName}");
        public readonly struct InjectionBuilder<TContext, TKey>(TKey key) where TKey : notnull {
            public IObservable<Unit> Using<TResult>(Func<TContext, IObservable<TResult>> factory) 
                => key.InjectWithContext(factory);
        }
        public readonly struct UniversalInjectionBuilder<TKey>(TKey key) where TKey : notnull {
            public InjectionBuilder<T1, T2, TKey> With<T1, T2>() => new(key);
            public InjectionBuilder<TContext, TKey> With<TContext>() => new(key);
            public IObservable<Unit> Using<T1, T2, TResult>(Func<T1, T2, IObservable<TResult>> factory) {
                return key.InjectWithContext<(T1, T2), TResult, TKey>( ctx => factory(ctx.Item1, ctx.Item2));
            }
            public IObservable<Unit> Using<TContext, TResult>(Func<TContext, IObservable<TResult>> factory) => key.InjectWithContext( factory);
        }

        public readonly struct InjectionBuilder<T1, T2, TKey>(TKey key) where TKey : notnull {
            public IObservable<Unit> Using<TResult>(Func<T1, T2, IObservable<TResult>> factory) {
                return key.InjectWithContext<(T1, T2), TResult, TKey>( ctx => factory(ctx.Item1, ctx.Item2));
            }
        }
        
    }
}