using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.CSharp;

namespace Xpand.Extensions.AssemblyExtensions{
    
    public static partial class AssemblyExtensions{
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

        public static Assembly NewAssembly(this AssemblyMetadata assemblyMetadata,params TypeMetada[] typeParameters){
            
            var compilerParameters = new CompilerParameters{
                CompilerOptions = "/t:library",
                OutputAssembly = assemblyMetadata.OutputAssembly
            };
            
            var codeProvider = CodeProvider.Value;
            compilerParameters.ReferencedAssemblies.Add(typeof(object).Assembly.Location);
            compilerParameters.ReferencedAssemblies.Add(typeof(AssemblyVersionAttribute).Assembly.Location);
            var references = typeParameters.SelectMany(_ =>
                _.Properties.Select(parameter => parameter.Type.Assembly.Location).Concat(
                    _.Properties.SelectMany(parameter =>
                        parameter.Attributes.Select(attribute => attribute.GetType().Assembly.Location)))).Distinct();
            compilerParameters.ReferencedAssemblies.AddRange(references.ToArray());
            var code = $"{assemblyMetadata.AssemblyCode()}{Environment.NewLine}{typeParameters.TypesCode()}";

            var compilerResults = codeProvider.CompileAssemblyFromSource(compilerParameters, code);
            if (compilerResults.Errors.Count > 0){
                var message = string.Join(Environment.NewLine,
                    compilerResults.Errors.Cast<CompilerError>().Select(error => error.ToString()));
                throw new Exception(message);
            }
            return compilerResults.CompiledAssembly;
        }

        private static string AssemblyCode(this AssemblyMetadata assemblyMetadata){
            var version = assemblyMetadata.Version??new Version("1.0.0.0");
            var versionCode = $@"
[assembly:{typeof(AssemblyVersionAttribute).FullName}(""{version}"")]
[assembly:{typeof(AssemblyFileVersionAttribute).FullName}(""{version}"")]
";
            return $"{versionCode}{assemblyMetadata.Attributes.AttributesCode()}";
        }

        private static string TypesCode(this IEnumerable<TypeMetada> typeParameters){
            return string.Join(Environment.NewLine, typeParameters.Select(_ => _.TypeCode()));
        }

        private static string AttributesCode(this IEnumerable<Attribute> attributes){
            return string.Join(Environment.NewLine, attributes.Select(AttributeCode));
        }

        private static string TypeCode(this TypeMetada metada){
            return $@"
public class {metada.Name}{{
    {metada.Properties.PropertiesCode()}
}}
";
        }

        private static string AttributeCode(this Attribute attribute){
            return null;
        }

        private static string PropertiesCode(this IEnumerable<PropertyMetadata> parameters){
            return string.Join(Environment.NewLine, parameters.Select(parameter => parameter.PropertyCode()));
        }

        private static string PropertyCode(this PropertyMetadata metadata){
            return $"{metadata.Attributes.AttributesCode()}{Environment.NewLine}public {metadata.Type.FullName} {metadata.Name} {{get;{(metadata.CanWrite ? "set;" : null)}}}";
        }
    }
    [PublicAPI]
    public class AssemblyMetadata{
        public AssemblyMetadata(){
            Attributes=new List<Attribute>();
        }

        public string OutputAssembly{ get; set; }
        public Version Version{ get; set; }
        public List<Attribute> Attributes{ get;  }
    }
    [PublicAPI]
    public class TypeMetada{
        public TypeMetada(string name,params PropertyMetadata[] propertyParameters){
            Name = name;
            Properties=new List<PropertyMetadata>();
            Properties.AddRange(propertyParameters);
        }

        public string Name{ get; set; }
        public List<PropertyMetadata> Properties{ get; }
    }
    [PublicAPI]
    public class PropertyMetadata{
        public PropertyMetadata(string name, Type type, bool canWrite, params Attribute[] attributes){
            Name = name;
            Type = type;
            CanWrite = canWrite;
            Attributes = new List<Attribute>(attributes);
        }

        public string Name{ get; set; }
        public Type Type{ get; set; }
        public bool CanWrite{ get; set; }
        public List<Attribute> Attributes{ get;  }
    }

}