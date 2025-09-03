using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.Json;

namespace Xpand.Extensions.Reactive.Transform.Json {
    public static class JsonExtensions {
        public static IObservable<JsonDocument> ToJsonDocument(this JsonElement element)
            => Observable.Using(() => new MemoryStream(),stream => Observable.Using(() => new Utf8JsonWriter(stream),writer => {
                element.WriteTo(writer);
                stream.Position = 0;
                return writer.FlushAsync().ToObservable()
                    .SelectMany(_ => JsonDocument.ParseAsync(stream).ToObservable());
            }));
    }
}