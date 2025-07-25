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
            return Channels.GetOrCreate(cacheKey, entry => {
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
            Console.WriteLine($"RpcChannel constructor called for <{typeof(TKey).Name}, {typeof(TRequest).Name}, {typeof(TResponse).Name}>"); 
        }

        internal IObservable<Unit> HandleRequests(TKey key, Func<TRequest, IObservable<TResponse>> handler)
            => _requests
                .Where(reqMsg => EqualityComparer<TKey>.Default.Equals(reqMsg.Key, key))
                .SelectMany(requestMsg =>
                    handler(requestMsg.Request)
                        .Take(1)
                        .Materialize() 
                        .Select(notification => new ResponseMessage(requestMsg.CorrelationId, notification))
                )
                .Do(_responses.OnNext)
                .ToUnit();

        internal IObservable<TResponse> MakeRequest(TKey key, TRequest request) 
            => Observable.Create<TResponse>(observer => {
                var correlationId = Guid.NewGuid();

                var responseSubscription = _responses
                    .Where(response => response.CorrelationId == correlationId)
                    .Take(1)
                    .Select(response => response.Result)
                    .Dematerialize() // Replaces the 'if/else' logic
                    .Subscribe(observer);

                _requests.OnNext(new RequestMessage(key, correlationId, request));

                return responseSubscription;
            });
    }
    
    public readonly struct RpcRequester<TKey> where TKey : notnull {
        private readonly TKey _key;
        internal RpcRequester(TKey key) => _key = key;

        public IObservable<TResponse> With<TResponse>()
            => RpcChannel.Get<TKey, Unit, TResponse>(_key).MakeRequest(_key, Unit.Default);

        public IObservable<TResponse> With<TRequest, TResponse>(TRequest request)
            => RpcChannel.Get<TKey, TRequest, TResponse>(_key).MakeRequest(_key, request);
    }

    public readonly struct RpcHandler<TKey> where TKey : notnull {
        private readonly TKey _key;
        internal RpcHandler(TKey key) => _key = key;

        public IObservable<Unit> With<TResponse>(IObservable<TResponse> source)
            => With<Unit, TResponse>(_ => source.Take(1));

        public IObservable<Unit> With<TResponse>(TResponse value)
            => With<Unit, TResponse>(_ => Observable.Return(value));

        public IObservable<Unit> With<TRequest, TResponse>(Func<TRequest, IObservable<TResponse>> handler)
            => RpcChannel.Get<TKey, TRequest, TResponse>(_key).HandleRequests(_key, handler);
    }
}