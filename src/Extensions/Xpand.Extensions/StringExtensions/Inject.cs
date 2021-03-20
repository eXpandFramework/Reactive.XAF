using System.Text;

namespace Xpand.Extensions.StringExtensions {
    public static partial class StringExtensions {
        public static string Inject(this string injectToString, int positionToInject, string stringToInject) {
            var builder = new StringBuilder();
            builder.Append(injectToString.Substring(0, positionToInject));
            builder.Append(stringToInject);
            builder.Append(injectToString.Substring(positionToInject));
            return builder.ToString();
        }
    }
}