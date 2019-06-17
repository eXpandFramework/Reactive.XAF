using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;

namespace Xpand.Source.Extensions.System.String{
    internal static partial class StringExtensions{
        internal static string ToLiteral(this string input){
            using (var writer = new StringWriter()){
                using (var provider = CodeDomProvider.CreateProvider("CSharp")){
                    provider.GenerateCodeFromExpression(new CodePrimitiveExpression(input), writer, null);
                    return writer.ToString();
                }
            }
        }
    }
}