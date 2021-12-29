using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        /// <summary>
        /// Periodically repeats the observable sequence exposing a responses or failures.
        /// </summary>
        /// <typeparam name="T">The type of the sequence response values.</typeparam>
        /// <param name="source">The source observable sequence to re-subscribe to after each <paramref name="period"/>.</param>
        /// <param name="period">The period of time to wait before subscribing to the <paramref name="source"/> sequence. Subsequent subscriptions will occur this period after the previous sequence completes.</param>
        /// <param name="scheduler">The <see cref="IScheduler"/> to use to schedule the polling.</param>
        /// <returns>Returns an infinite observable sequence of values or errors.</returns>
        public static IObservable<Try<T>> Poll<T>(this IObservable<T> source, TimeSpan period, IScheduler scheduler) 
            => Observable.Timer(period, scheduler)
                .SelectMany(_ => source) 
                .Select(Try<T>.Create) 
                .Catch<Try<T>, Exception>(ex => Observable.Return(Try<T>.Fail(ex))) 
                .Repeat();
    }

    public abstract class Try<T> {
        private Try() { }
        public static Try<T> Create(T value) => new Success(value);
        public static Try<T> Fail(Exception value) => new Error(value);
        public abstract TResult Switch<TResult>(Func<T, TResult> caseValue, Func<Exception, TResult> caseError);
        public abstract void Switch(Action<T> caseValue, Action<Exception> caseError);
        private sealed class Success : Try<T>, IEquatable<Success> {
            private readonly T _value;
            public Success(T value) => _value = value;
            public override TResult Switch<TResult>(Func<T, TResult> caseValue, Func<Exception, TResult> caseError) => caseValue(_value);
            public override void Switch(Action<T> caseValue, Action<Exception> caseError) => caseValue(_value);
            public bool Equals(Success other) 
                => ReferenceEquals(other, this) || other != null && EqualityComparer<T>.Default.Equals(_value, other._value);
            public override bool Equals(object obj) => Equals(obj as Success);
            public override int GetHashCode() => EqualityComparer<T>.Default.GetHashCode(_value);
            public override string ToString() => string.Format(CultureInfo.CurrentCulture, "Success({0})", _value);
        }
        private sealed class Error : Try<T>, IEquatable<Error> {
            private readonly Exception _exception;
            public Error(Exception exception) => _exception = exception ?? throw new ArgumentNullException(nameof(exception));
            public override TResult Switch<TResult>(Func<T, TResult> caseValue, Func<Exception, TResult> caseError) => caseError(_exception);
            public override void Switch(Action<T> caseValue, Action<Exception> caseError) => caseError(_exception);
            public bool Equals(Error other) => ReferenceEquals(other, this) || other != null && Equals(_exception, other._exception);
            private static bool Equals(Exception a, Exception b) 
                => a == null && b == null || a != null && b != null &&
                    (a.GetType() == b.GetType() && (string.Equals(a.Message, b.Message) && Equals(a.InnerException, b.InnerException)));
            public override bool Equals(object obj) => Equals(obj as Error);
            public override int GetHashCode() => EqualityComparer<Exception>.Default.GetHashCode(_exception);
            public override string ToString() => string.Format(CultureInfo.CurrentCulture, "Error({0})", _exception);
        }
    }
}