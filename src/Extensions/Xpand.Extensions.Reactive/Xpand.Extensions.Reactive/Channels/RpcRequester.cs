using System;
using System.Reactive;

namespace Xpand.Extensions.Reactive{
    public readonly struct RpcRequester<TKey> where TKey : notnull {
        private readonly TKey _key;
        internal RpcRequester(TKey key) => _key = key;

        public IObservable<TResponse> With<TResponse>() {
            LogFast($"Requester for key '{_key}' making a request with no payload (Unit).");
            return RpcChannel.Get<TKey, Unit, TResponse>(_key).MakeRequest(_key, Unit.Default);
        }

        public IObservable<TResponse> With<TRequest, TResponse>(TRequest request) {
            LogFast($"Requester for key '{_key}' making a request with payload: {request}");
            return RpcChannel.Get<TKey, TRequest, TResponse>(_key).MakeRequest(_key, request);
        }
    }
}