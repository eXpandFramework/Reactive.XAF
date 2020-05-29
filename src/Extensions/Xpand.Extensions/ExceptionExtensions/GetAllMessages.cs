using System;
using System.Linq;
using System.Reflection;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.ExceptionExtensions{
    public static partial class ExceptionExtensions{
        public static string GetAllInfo(this Exception exception){
            if (exception is AggregateException aex){
                var flatten = aex.Flatten();
                return flatten.ToString();
            }
            var messages = exception.FromHierarchy(ex => ex.InnerException).Select(ex => {
                var s = ex.ToString();
                if (ex is ReflectionTypeLoadException reflectionTypeLoadException){
                    s+=$"{Environment.NewLine}{string.Join(Environment.NewLine,reflectionTypeLoadException.LoaderExceptions.Select(_ => _.GetAllInfo()))}";
                }
                return s;
            });
            return string.Join(Environment.NewLine, messages);
        }
    }
}