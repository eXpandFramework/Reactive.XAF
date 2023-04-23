using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xpand.Extensions.JsonExtensions;
using Xpand.Extensions.Reactive.Transform.System.Net;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.Reactive.Transform.System.Text.Json {
    public static class JsonExtension {
        [Obsolete]
        public static IObservable<T> ToJsonElement<T>(this JsonNode jsonNode, Func<JsonElement, IObservable<T>> selector)
            => jsonNode.ToString().ToJsonElement(selector);
        
        public static IObservable<T> ToJsonElement<T>(this string source, Func<JsonElement, IObservable<T>> selector)
            => Observable.Using(() => source.MemoryStream(),stream => stream.ToJsonDocument(document => selector(document.RootElement))).SelectMany();

        public static IObservable<(T[] objects, JsonDocument document)> ToJsonDocument<T>(this IObservable<Stream> source, Func<JsonDocument, IObservable<T>> selector)
            => source.SelectMany(stream => stream.ToJsonDocument(selector));
        public static IObservable<(T[] objects, JsonDocument document)> ToJsonDocument<T>(this Stream stream,Func<JsonDocument,IObservable<T>> selector) 
            => JsonDocument.ParseAsync(stream).ToObservable(Scheduler.Immediate).SelectMany(document => selector(document)
                    .BufferUntilCompleted().Pair(document).FinallySafe(document.Dispose));

        public static bool IsDisposed(this JsonElement element) {
            try {
                var _ = element.YieldJsonElementArray();
                return false;
            }
            catch (ObjectDisposedException) {
                return true;
            }
        }

        public static bool IsDisposed(this JsonDocument document) 
            => document.RootElement.IsDisposed();
    }
}