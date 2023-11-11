using System;
using System.Linq;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.ExceptionExtensions {
    public static partial class ExceptionExtensions {
        public static string ReverseStackTrace(this Exception exception) 
            => $"{exception.FromHierarchy(exception1 => exception1.InnerException).Select(exception1 => $"{exception1.StackTrace}").Reverse().Join(Environment.NewLine)}";
    }
}