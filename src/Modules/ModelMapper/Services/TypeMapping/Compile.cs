using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using Microsoft.CSharp;
using Mono.Cecil;


namespace Xpand.XAF.Modules.ModelMapper.Services.TypeMapping{
    public static partial class TypeMappingService{
        static string OutputAssembly =>
            $@"{Path.GetDirectoryName(typeof(TypeMappingService).Assembly.Location)}\{ModelMapperAssemblyName}{MapperAssemblyName}{ModelExtendingService.Platform}.dll";


        private static Assembly Compile(this IEnumerable<string> references, string code){
            var codeProvider = new CSharpCodeProvider();
            var compilerParameters = new CompilerParameters{
                CompilerOptions = "/t:library /optimize",
                OutputAssembly = OutputAssembly
            };
            
            var strings = references.Distinct().ToArray();
            compilerParameters.ReferencedAssemblies.AddRange(strings);

            var compilerResults = codeProvider.CompileAssemblyFromSource(compilerParameters, code);
            if (compilerResults.Errors.Count > 0){
                var message = String.Join(Environment.NewLine,
                    compilerResults.Errors.Cast<CompilerError>().Select(error => error.ToString()));
                throw new Exception(message);
            }
            var compilerResultsCompiledAssembly = compilerResults.CompiledAssembly;
            return compilerResultsCompiledAssembly;
        }

        private static bool TypeFromPath(this (Type type,IModelMapperConfiguration configuration) data){
            if (File.Exists(OutputAssembly)){
                using (var assembly = AssemblyDefinition.ReadAssembly(OutPutAssembly)){
                    if (assembly.IsMapped(data) && !assembly.VersionChanged() && !assembly.ConfigurationChanged(data)){
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool ConfigurationChanged(this AssemblyDefinition assembly,(Type type, IModelMapperConfiguration configuration) data){
            var configurationChanged = assembly.CustomAttributes.Any(attribute => {
                if (attribute.AttributeType.FullName != typeof(ModelMapperModelConfigurationAttribute).FullName) return false;
                int hashCode = 0;
                if (data.configuration != null) hashCode = data.configuration.GetHashCode();
                var typeMatch = ((string) attribute.ConstructorArguments.First().Value) == data.type.FullName;
                if (typeMatch){
                    return !attribute.ConstructorArguments.Last().Value.Equals(hashCode);
                }

                return false;
            });
            return configurationChanged;
        }

        private static bool VersionChanged(this AssemblyDefinition assembly){
            var versionAttribute = assembly.CustomAttributes.First(attribute =>
                attribute.AttributeType.FullName == typeof(AssemblyFileVersionAttribute).FullName);
            return Version.Parse(versionAttribute.ConstructorArguments.First().Value.ToString()) !=_modelMapperModuleVersion;
        }

        private static bool IsMapped(this AssemblyDefinition assembly,(Type type, IModelMapperConfiguration configuration) data){
            var typeVersion = data.type.Assembly.GetName().Version;
            var modelMapperServiceAttributes = assembly.CustomAttributes.Where(attribute => attribute.AttributeType.FullName == typeof(ModelMapperServiceAttribute).FullName).ToArray();
            return modelMapperServiceAttributes.Any(attribute => {
                var mappedTypeVersion = Version.Parse((string) attribute.ConstructorArguments.Last().Value);
                var mappedType = (string) attribute.ConstructorArguments.First().Value;
                return mappedTypeVersion == typeVersion && mappedType==data.type.FullName;

            });
        }

        private static IObservable<Type> Compile(this IObservable<(string code,IEnumerable<string> references)> source){
            return source.SelectMany(_ => {
                var assembly = _.references.Compile(_.code);
                var types = assembly.GetTypes().Where(type => typeof(IModelModelMap).IsAssignableFrom(type));
                return types;
            });
        }
    }
}