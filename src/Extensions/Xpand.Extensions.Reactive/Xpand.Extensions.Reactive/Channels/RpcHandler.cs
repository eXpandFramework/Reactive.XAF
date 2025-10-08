using System;
using System.Reactive;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive{
    public readonly struct RpcHandler<TKey> where TKey : notnull {
        private readonly TKey _key;
        internal RpcHandler(TKey key) => _key = key;

        public IObservable<Unit> With<TResponse>(IObservable<TResponse> source) {
            LogFast($"Handler for key '{_key}' is being set up with an IObservable<TResponse> source.");
            return With<Unit, TResponse>(_ => source.Take(1));
        }

        public IObservable<Unit> With<TResponse>(TResponse value) {
            LogFast($"Handler for key '{_key}' is being set up with a single value: {value}");
            return With<Unit, TResponse>(_ => Observable.Return(value));
        }

        public IObservable<Unit> With<TRequest, TResponse>(Func<TRequest, IObservable<TResponse>> handler) {
            LogFast($"Handler for key '{_key}' is being set up with a function delegate.");
            return RpcChannel.Get<TKey, TRequest, TResponse>(_key).HandleRequests(_key, handler);
        }
    }
}