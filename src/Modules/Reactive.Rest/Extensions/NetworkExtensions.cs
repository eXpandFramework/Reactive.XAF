﻿using System;
using System.Net.Http;
using System.Reactive.Linq;
using Xpand.Extensions.Reactive.Transform.System.Net;

namespace Xpand.XAF.Modules.Reactive.Rest.Extensions {
    internal static class NetworkExtensions {
        internal static IObservable<T> Send<T>(this HttpMethod httpMethod, string requestUrl, T obj, string key = null,
            string secret = null, Func<HttpResponseMessage, IObservable<T>> deserializeResponse = null, TimeSpan? pollInterval = null) where T : class, new() 
            => httpMethod.Send(requestUrl,RestService.CacheStorage,obj,key,secret,deserializeResponse,pollInterval)
                .Select(arg => arg);
    }
}