using System;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using Xpand.Extensions.Linq;

namespace Xpand.Extensions.Exception{
    public static partial class ExceptionExtensions{
        public static string GetAllInfo(this global::System.Exception exception){
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