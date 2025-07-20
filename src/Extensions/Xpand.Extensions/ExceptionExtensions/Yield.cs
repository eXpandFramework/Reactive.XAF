using System;
using System.Collections.Generic;
using System.Linq;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.ExceptionExtensions {
    public static partial class ExceptionExtensions {
        public static IEnumerable<T> Yield<T>(this Exception exception)
            => exception is T t ? t.YieldItem() : exception.InnerException is T innerException
                    ? innerException.YieldItem() : exception is AggregateException aggregateException
                        ? aggregateException.InnerExceptions.OfType<T>() : [];
    }
}