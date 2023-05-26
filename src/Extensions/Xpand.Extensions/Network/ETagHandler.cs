using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.Network{
    public class ETagHandler : DelegatingHandler {
        private static readonly ConcurrentDictionary<Uri, string> ETagCache = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            if (request.Method == HttpMethod.Get && ETagCache.TryGetValue(request.RequestUri!, out var eTag))
                request.Headers.TryAddWithoutValidation("If-None-Match", eTag);
            var response = await base.SendAsync(request, cancellationToken);
            switch (response.StatusCode){
                case HttpStatusCode.OK when response.Headers.TryGetValues("ETag", out var eTags):{
                    var eTagValue = eTags.FirstOrDefault();
                    ETagCache.AddOrUpdate(request.RequestUri, eTagValue, (_, _) => eTagValue);
                    break;
                }
                case HttpStatusCode.NotModified:
                    response.Content = new ByteArrayContent(EmptyContent);
                    response.StatusCode = HttpStatusCode.OK;
                    break;
            }
            return response;
        }

        public static byte[] EmptyContent { get; } = "[]".Bytes();
    }
}