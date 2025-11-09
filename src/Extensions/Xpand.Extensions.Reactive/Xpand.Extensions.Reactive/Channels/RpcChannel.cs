using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Caching.Memory;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Channels {
    public static class RpcChannel {
        [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local")]
        private record CacheKey(Type KeyType, Type RequestType, Type ResponseType, object KeyObject) {
            public override string ToString()
                => $"{nameof(CacheKey)} {{ {nameof(KeyType)} = {KeyType.FullName}, {nameof(RequestType)} = {RequestType.FullName}, {nameof(ResponseType)} = {ResponseType.FullName}, {nameof(KeyObject)} = {KeyString(KeyObject)} }}";
        }
        public static TimeSpan SlidingExpiration { get; set; } = TimeSpan.FromMinutes(10);
        private static readonly MemoryCache Channels = new(new MemoryCacheOptions());
        
        public static void Reset() {
            Channels.Compact(1.0);
            LogFast($"{nameof(RpcChannel)} cache cleared.");
        }
        public static IObservable<Unit> HandleRequest<TKey, TResponse>(this TKey key, TResponse value) where TKey : notnull
            => new RpcHandler<TKey>(key).With(value);

        public static IObservable<Unit> HandleRequest<TKey, TResponse>(this TKey key, IObservable<TResponse> source) where TKey : notnull
            => new RpcHandler<TKey>(key).With(source);
        public static RpcHandler<TKey> HandleRequest<TKey>(this TKey key) where TKey : notnull
            => new(key);

        public static RpcRequester<TKey> MakeRequest<TKey>(this TKey key) where TKey : notnull
            => new(key);
        
        internal static string KeyString(object key) => $"{key.GetType().Name} - {key.GetHashCode()}";

        internal static RpcChannel<TKey, TRequest, TResponse> Get<TKey, TRequest, TResponse>(TKey key) where TKey : notnull {
            var cacheKey = new CacheKey(typeof(TKey), typeof(TRequest), typeof(TResponse), key);
            LogFast($"Attempting to get or create RpcChannel with stable cache cacheKey: {cacheKey}");
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
        private readonly ISubject<RequestMessage> _requests = new Subject<RequestMessage>();
        private readonly ISubject<ResponseMessage> _responses = new Subject<ResponseMessage>();

        public RpcChannel(){
            LogFast($"RpcChannel constructor called for <{typeof(TKey).Name}, {typeof(TRequest).Name}, {typeof(TResponse).Name}>"); 
        }

        
        internal IObservable<Unit> HandleRequests(TKey key, Func<TRequest, IObservable<TResponse>> handler)
            => _requests.AsObservable().ObserveOn(TaskPoolScheduler.Default)
                .Do(reqMsg => LogFast($"Request received on channel for key '{RpcChannel.KeyString(reqMsg.Key)}', CorrelationId: {reqMsg.CorrelationId}"))
                .Where(reqMsg => EqualityComparer<TKey>.Default.Equals(reqMsg.Key, key))
                .Do(reqMsg => LogFast($"Request handler matched for key '{RpcChannel.KeyString(key)}', CorrelationId: {reqMsg.CorrelationId}"))
                .SelectMany(requestMsg =>
                    handler(requestMsg.Request)
                        .Take(1)
                        .Materialize()
                        .SelectMany(notification => {
                            LogFast($"[TID:{Environment.CurrentManagedThreadId}] ENTERING error report SelectMany for CorrelationId: {requestMsg.CorrelationId}");
                            if (notification.Kind != NotificationKind.OnError) {
                                return Observable.Return(notification);
                            }

                            LogFast($"Handler for key '{RpcChannel.KeyString(key)}' failed. Reporting to AppDomain error channel for CorrelationId: {requestMsg.CorrelationId}");
                            
                            var reportErrorStream = AppDomain.CurrentDomain.MakeRequest()
                                .With<Exception, Unit>(notification.Exception)
                                .Timeout(TimeSpan.FromSeconds(1), Observable.Throw<Unit>(new InvalidOperationException(
                                    "RpcChannel handler failed, but no subscriber was found for the AppDomain error channel. " +
                                    "You must subscribe to the error channel at application startup to handle suppressed errors.",
                                    notification.Exception)))
                                ;

                            return reportErrorStream
                                .Do(_ => LogFast($"[TID:{Environment.CurrentManagedThreadId}] EXITING error report SelectMany for CorrelationId: {requestMsg.CorrelationId}"))
                                .Select(_ => notification);
                        })
                        .Do(notification => LogFast($"Handler for key '{RpcChannel.KeyString(key)}' produced a notification ({notification.Kind}) for CorrelationId: {requestMsg.CorrelationId}"))
                        .Select(notification => new ResponseMessage(requestMsg.CorrelationId, notification))
                )
                .Do(responseMsg => {
                    LogFast($"[TID:{Environment.CurrentManagedThreadId}] ATTEMPTING to publish request for CorrelationId: {responseMsg.CorrelationId}");
                    LogFast($"Sending response for CorrelationId: {responseMsg.CorrelationId} with result kind {responseMsg.Result.Kind}");
                    _responses.OnNext(responseMsg);
                    LogFast($"[TID:{Environment.CurrentManagedThreadId}] SUCCESSFULLY published request for CorrelationId: {responseMsg.CorrelationId}");
                })
                .ToUnit();

        internal IObservable<TResponse> MakeRequest(TKey key, TRequest request) 
            => Observable.Create<TResponse>(observer => {
                var correlationId = Guid.NewGuid();
                LogFast($"Making request with key '{RpcChannel.KeyString(key)}', generated CorrelationId: {correlationId}");

                var responseSubscription = _responses
                    .Where(response => response.CorrelationId == correlationId)
                    .Do(response => LogFast($"Response received for CorrelationId: {correlationId}, Result Kind: {response.Result.Kind}"))
                    .Take(1)
                    .Select(response => response.Result)
                    .Dematerialize() 
                    .Subscribe(observer);

                LogFast($"Publishing request message to subject for key '{RpcChannel.KeyString(key)}', CorrelationId: {correlationId}");
                _requests.OnNext(new RequestMessage(key, correlationId, request));

                return responseSubscription;
            });
    }
    
    public readonly struct RpcRequester<TKey> where TKey : notnull {
        private readonly TKey _key;
        internal RpcRequester(TKey key) => _key = key;

        public IObservable<TResponse> With<TResponse>() {
            LogFast($"Requester for key '{RpcChannel.KeyString(_key)}' making a request with no payload (Unit).");
            return RpcChannel.Get<TKey, Unit, TResponse>(_key).MakeRequest(_key, Unit.Default);
        }

        public IObservable<TResponse> With<TRequest, TResponse>(TRequest request) {
            LogFast($"Requester for key '{RpcChannel.KeyString(_key)}' making a request with payload: {request}");
            return RpcChannel.Get<TKey, TRequest, TResponse>(_key).MakeRequest(_key, request);
        }
    }

    public readonly struct RpcHandler<TKey> where TKey : notnull {
        private readonly TKey _key;
        internal RpcHandler(TKey key) => _key = key;

        public IObservable<Unit> With<TResponse>(IObservable<TResponse> source) {
            LogFast($"Handler for key '{RpcChannel.KeyString(_key)}' is being set up with an IObservable<TResponse> source.");
            return With<Unit, TResponse>(_ => source.Take(1));
        }

        public IObservable<Unit> With<TResponse>(TResponse value) {
            LogFast($"Handler for key '{RpcChannel.KeyString(_key)}' is being set up with a single value: {value}");
            return With<Unit, TResponse>(_ => Observable.Return(value));
        }

        public IObservable<Unit> With<TRequest, TResponse>(Func<TRequest, IObservable<TResponse>> handler) {
            LogFast($"Handler for key '{RpcChannel.KeyString(_key)}' is being set up with a function delegate.");
            return RpcChannel.Get<TKey, TRequest, TResponse>(_key).HandleRequests(_key, handler);
        }

        
    }
}