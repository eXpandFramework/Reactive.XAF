using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;

namespace Xpand.Source.Extensions.System.String{
    internal static partial class StringExtensions{
        private static readonly CodeDomProvider CodeDomProvider;

        static StringExtensions(){
            CodeDomProvider = CodeDomProvider.CreateProvider("CSharp");
        }
        internal static string ToLiteral(this string input){
            using (var writer = new StringWriter()){
                CodeDomProvider.GenerateCodeFromExpression(new CodePrimitiveExpression(input), writer, null);
                return writer.ToString();
            }
        }
    }
}