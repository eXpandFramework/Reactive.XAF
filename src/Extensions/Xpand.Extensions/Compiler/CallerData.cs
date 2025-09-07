using System.Linq;
using System.Runtime.CompilerServices;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.Compiler {
    public static class CallerDataService {
        public static (string memberName, string filePath, int lineNumber) CallerData(
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
            => (memberName, filePath, lineNumber);
        
        public static string CallerMember([CallerMemberName] string memberName = "")
            => memberName;
        
        public static (string memberName, string filePath, int lineNumber) CallerData(object[] context,[CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
            => (context.Prepend(memberName).JoinCommaSpace(), filePath, lineNumber);
    }
}