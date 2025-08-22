using System;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        private static readonly Regex MethodNameRegex = new(@"(?:\.)?(\w+)\s*\($", RegexOptions.Compiled | RegexOptions.RightToLeft);
        const string Name = "Unnamed";
        public static NamedStream<T> WithName<T>(this IObservable<T> source, [CallerArgumentExpression("source")] string expression = null) {
            if (expression == null) return new NamedStream<T> { Source = source, Name = Name };
            var match = MethodNameRegex.Match(expression);
            return new NamedStream<T> { Source = source, Name = match.Success ? match.Groups[1].Value : expression.Split('.').Last() };
        }
    }
    
    public class NamedStream<T>:INamedStream {
        IObservable<object> INamedStream.Source => Source.Select(item => (object)item);
        public IObservable<T> Source { get; init; }
        public string Name { get; init; }
    }
    public interface INamedStream {
        string Name { get; }
        IObservable<object> Source { get; }
    }
}