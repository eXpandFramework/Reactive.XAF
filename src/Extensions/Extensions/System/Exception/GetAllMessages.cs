using System;
using System.Linq;
using Xpand.Source.Extensions.Linq;

namespace Xpand.Source.Extensions.System.Exception{
    internal static class ExceptionExtensions{
        public static string GetAllMessages(this global::System.Exception exception){
            var messages = exception.FromHierarchy(ex => ex.InnerException).Select(ex => ex.Message);
            return string.Join(Environment.NewLine, messages);
        }
    }
}