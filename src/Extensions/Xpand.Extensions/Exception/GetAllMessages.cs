using System;
using System.Linq;
using Xpand.Extensions.Linq;

namespace Xpand.Extensions.Exception{
    public static class ExceptionExtensions{
        public static string GetAllMessages(this global::System.Exception exception){
            var messages = exception.FromHierarchy(ex => ex.InnerException).Select(ex => ex.Message);
            return string.Join(Environment.NewLine, messages);
        }
    }
}