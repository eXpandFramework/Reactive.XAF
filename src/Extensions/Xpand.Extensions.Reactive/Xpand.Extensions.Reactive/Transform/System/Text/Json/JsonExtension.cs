using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.Json;
using Xpand.Extensions.JsonExtensions;
using Xpand.Extensions.Reactive.Transform.System.Net;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.StringExtensions;

namespace Xpand.Extensions.Reactive.Transform.System.Text.Json {
    public static class JsonExtension {
        public static IObservable<JsonElement> SelectMany(this IObservable<JsonElement> source)
            => source.SelectMany(document => document.SelectMany());
        public static IObservable<T> WhenJsonElement<T>(this string source, Func<JsonElement, IObservable<T>> selector)
            => Observable.Using(() => source.MemoryStream(),stream => stream.WhenJsonDocument(document => selector(document.RootElement))).SelectMany();

        public static IObservable<(T[] objects, JsonDocument document)> WhenJsonDocument<T>(this IObservable<Stream> source, Func<JsonDocument, IObservable<T>> selector)
            => source.SelectMany(stream => stream.WhenJsonDocument(selector));
        public static IObservable<(T[] objects, JsonDocument document)> WhenJsonDocument<T>(this Stream stream,Func<JsonDocument,IObservable<T>> selector) 
            => JsonDocument.ParseAsync(stream).ToObservable().SelectMany(document => selector(document)
                    .BufferUntilCompleted().Pair(document)
                    .FinallySafe(document.Dispose)
            );
        
        public static IObservable<JsonDocument> WhenJsonDocument(this Stream stream) 
            => JsonDocument.ParseAsync(stream).ToObservable();
        public static IObservable<JsonDocument> WhenJsonDocument(this byte[] bytes) 
            => Observable.Using(() => new MemoryStream(bytes),stream => JsonDocument.ParseAsync(stream).ToObservable());


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