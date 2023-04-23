using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static string Inject(this string injectToString, int positionToInject, string stringToInject) 
            => new[] { injectToString.Substring(0, positionToInject), stringToInject,
                injectToString.Substring(positionToInject) }.JoinString();
    }
}