using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CSharp;

namespace Xpand.Extensions.Reflection.Assembly{
    public static class AssemblyExtensions{
        public static Lazy<CSharpCodeProvider> CodeProvider { get; } = new Lazy<CSharpCodeProvider>(() => {
            var csc = new CSharpCodeProvider();
            var settings = csc
                .GetType()
                .GetField("_compilerSettings", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(csc);

            var path = settings?.GetType()
                .GetField("_compilerFullPath", BindingFlags.Instance | BindingFlags.NonPublic);

            path?.SetValue(settings, ((string)path.GetValue(settings)).Replace(@"bin\roslyn\", @"roslyn\"));

            return csc;
        });

        public static System.Reflection.Assembly New(this AssemblyParameter assemblyParameter,params TypeParameter[] typeParameters){
            
            var compilerParameters = new CompilerParameters{
                CompilerOptions = "/t:library",
                OutputAssembly = assemblyParameter.OutputAssembly
            };
            
            var codeProvider = CodeProvider.Value;
            compilerParameters.ReferencedAssemblies.Add(typeof(object).Assembly.Location);
            compilerParameters.ReferencedAssemblies.Add(typeof(AssemblyVersionAttribute).Assembly.Location);
            var references = typeParameters.SelectMany(_ =>
                _.Properties.Select(parameter => parameter.Type.Assembly.Location).Concat(
                    _.Properties.SelectMany(parameter =>
                        parameter.Attributes.Select(attribute => attribute.GetType().Assembly.Location)))).Distinct();
            compilerParameters.ReferencedAssemblies.AddRange(references.ToArray());
            var code = $"{assemblyParameter.AssemblyCode()}{Environment.NewLine}{typeParameters.TypesCode()}";

            var compilerResults = codeProvider.CompileAssemblyFromSource(compilerParameters, code);
            if (compilerResults.Errors.Count > 0){
                var message = System.String.Join(Environment.NewLine,
                    compilerResults.Errors.Cast<CompilerError>().Select(error => error.ToString()));
                throw new System.Exception(message);
            }
            return compilerResults.CompiledAssembly;
        }

        private static string AssemblyCode(this AssemblyParameter assemblyParameter){
            var version = assemblyParameter.Version??new Version("1.0.0.0");
            var versionCode = $@"
[assembly:{typeof(AssemblyVersionAttribute).FullName}(""{version}"")]
[assembly:{typeof(AssemblyFileVersionAttribute).FullName}(""{version}"")]
";
            return $"{versionCode}{assemblyParameter.Attributes.AttributesCode()}";
        }

        private static string TypesCode(this IEnumerable<TypeParameter> typeParameters){
            return string.Join(Environment.NewLine, typeParameters.Select(_ => _.TypeCode()));
        }

        private static string AttributesCode(this IEnumerable<Attribute> attributes){
            return string.Join(Environment.NewLine, attributes.Select(AttributeCode));
        }

        private static string TypeCode(this TypeParameter parameter){
            return $@"
public class {parameter.Name}{{
    {parameter.Properties.PropertiesCode()}
}}
";
        }

        private static string AttributeCode(this Attribute attribute){
            return null;
        }

        private static string PropertiesCode(this IEnumerable<PropertyParameter> parameters){
            return string.Join(Environment.NewLine, parameters.Select(parameter => parameter.PropertyCode()));
        }

        private static string PropertyCode(this PropertyParameter parameter){
            return $"{parameter.Attributes.AttributesCode()}{Environment.NewLine}public {parameter.Type.FullName} {parameter.Name} {{get;{(parameter.CanWrite ? "set;" : null)}}}";
        }
    }

    public class TypeParameter{
    }

    public class AssemblyParameter{
        public string OutputAssembly{ get; set; }
    }
}