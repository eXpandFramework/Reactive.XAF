using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Caching.Memory;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive {
    public static class RpcChannel {
        public static TimeSpan SlidingExpiration { get; set; } = TimeSpan.FromMinutes(10);
        private static readonly MemoryCache Channels = new(new MemoryCacheOptions());
        public static IObservable<Unit> HandleRequest<TKey, TResponse>(this TKey key, TResponse value) where TKey : notnull
            => new RpcHandler<TKey>(key).With(value);

        public static IObservable<Unit> HandleRequest<TKey, TResponse>(this TKey key, IObservable<TResponse> source) where TKey : notnull
            => new RpcHandler<TKey>(key).With(source);
        public static RpcHandler<TKey> HandleRequest<TKey>(this TKey key) where TKey : notnull
            => new(key);

        public static RpcRequester<TKey> MakeRequest<TKey>(this TKey key) where TKey : notnull
            => new(key);

        internal static RpcChannel<TKey, TRequest, TResponse> Get<TKey, TRequest, TResponse>(TKey key) where TKey : notnull {
            var cacheKey = $"RpcChannel<{typeof(TKey).FullName},{typeof(TRequest).FullName},{typeof(TResponse).FullName}>_({key})";
            LogFast($"Attempting to get or create RpcChannel with cache key: {cacheKey}");
            return Channels.GetOrCreate(cacheKey, entry => {
                LogFast($"Cache miss for key: {cacheKey}. Creating new RpcChannel.");
                entry.SetSlidingExpiration(SlidingExpiration);
                return new RpcChannel<TKey, TRequest, TResponse>();
            });
        }
    }

    internal class RpcChannel<TKey, TRequest, TResponse> where TKey : notnull {
        private record RequestMessage(TKey Key, Guid CorrelationId, TRequest Request);
        private record ResponseMessage(Guid CorrelationId, Notification<TResponse> Result);
        private readonly ISubject<RequestMessage> _requests = Subject.Synchronize(new Subject<RequestMessage>());
        private readonly ISubject<ResponseMessage> _responses=Subject.Synchronize(new Subject<ResponseMessage>());

        public RpcChannel(){
            LogFast($"RpcChannel constructor called for <{typeof(TKey).Name}, {typeof(TRequest).Name}, {typeof(TResponse).Name}>"); 
        }

        internal IObservable<Unit> HandleRequests(TKey key, Func<TRequest, IObservable<TResponse>> handler)
            => _requests
                .Do(reqMsg => LogFast($"Request received on channel for key '{reqMsg.Key}', CorrelationId: {reqMsg.CorrelationId}"))
                .Where(reqMsg => EqualityComparer<TKey>.Default.Equals(reqMsg.Key, key))
                .Do(reqMsg => LogFast($"Request handler matched for key '{key}', CorrelationId: {reqMsg.CorrelationId}"))
                .SelectMany(requestMsg =>
                    handler(requestMsg.Request)
                        .Take(1)
                        .Materialize()
                        .Do(notification => LogFast($"Handler for key '{key}' produced a notification ({notification.Kind}) for CorrelationId: {requestMsg.CorrelationId}"))
                        .Select(notification => new ResponseMessage(requestMsg.CorrelationId, notification))
                )
                .Do(responseMsg => {
                    LogFast($"Sending response for CorrelationId: {responseMsg.CorrelationId} with result kind {responseMsg.Result.Kind}");
                    _responses.OnNext(responseMsg);
                })
                .ToUnit();

        internal IObservable<TResponse> MakeRequest(TKey key, TRequest request) 
            => Observable.Create<TResponse>(observer => {
                var correlationId = Guid.NewGuid();
                LogFast($"Making request with key '{key}', generated CorrelationId: {correlationId}");

                var responseSubscription = _responses
                    .Where(response => response.CorrelationId == correlationId)
                    .Do(response => LogFast($"Response received for CorrelationId: {correlationId}, Result Kind: {response.Result.Kind}"))
                    .Take(1)
                    .Select(response => response.Result)
                    .Dematerialize() 
                    .Subscribe(observer);

                LogFast($"Publishing request message to subject for key '{key}', CorrelationId: {correlationId}");
                _requests.OnNext(new RequestMessage(key, correlationId, request));

                return responseSubscription;
            });
    }
    
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