using System;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static INamedStream ToNamedStream<T>(this IObservable<T> source, [CallerArgumentExpression(nameof(source))] string name = null,
            [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
            => new NamedStream<T> { Name = Combine.Combine.GetStepName(name), Source = source, FilePath = filePath, LineNumber = lineNumber };
    }
    
    public class NamedStream<T>:INamedStream {
        IObservable<object> INamedStream.Source => Source.Select(item => (object)item);
        public IObservable<T> Source { get; init; }
        public string Name { get; init; }
        public string FilePath { get; init; }
        public int LineNumber { get; init; }
    }
    public interface INamedStream {
        string Name { get; }
        string FilePath { get; }
        int LineNumber { get; }
        IObservable<object> Source { get; }
    }
}