using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using DevExpress.ExpressApp.Model;
using Microsoft.CSharp;
using Mono.Cecil;
using Xpand.Source.Extensions.MonoCecil;

namespace Xpand.XAF.Modules.ModelMapper.Services.TypeMapping{
    public static partial class TypeMappingService{
        static string OutputAssembly =>
            $@"{Path.GetDirectoryName(typeof(TypeMappingService).Assembly.Location)}\{TypeMappingService.ModelMapperAssemblyName}{TypeMappingService.MapperAssemblyName}{ModelExtendingService.Platform}.dll";


        private static Assembly Compile(this IEnumerable<Assembly> references, string code){
            var codeProvider = new CSharpCodeProvider();
            var compilerParameters = new CompilerParameters{
                CompilerOptions = "/t:library",
                OutputAssembly = OutputAssembly
            };
            
            compilerParameters.ReferencedAssemblies.AddRange(references.Select(_ => _.Location).Distinct().ToArray());
            compilerParameters.ReferencedAssemblies.Add(typeof(IModelNode).Assembly.Location);

            var compilerResults = codeProvider.CompileAssemblyFromSource(compilerParameters, code);
            if (compilerResults.Errors.Count > 0){
                var message = String.Join(Environment.NewLine,
                    compilerResults.Errors.Cast<CompilerError>().Select(error => error.ToString()));
                throw new Exception(message);
            }

            return compilerResults.CompiledAssembly;
        }

        private static bool TypeFromPath(this (Type type,IModelMapperConfiguration configuration) data){
            if (File.Exists(OutputAssembly)){
                using (var assemblyDefinition = AssemblyDefinition.ReadAssembly(OutputAssembly)){
                    if (assemblyDefinition.IsMapped(data) && !assemblyDefinition.VersionChanged() && !assemblyDefinition.ConfigurationChanged(data)){
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool ConfigurationChanged(this AssemblyDefinition assemblyDefinition,(Type type, IModelMapperConfiguration configuration) data){
            var configurationChanged = assemblyDefinition.CustomAttributes.Any(attribute => {
                if (attribute.AttributeType.ToType() != typeof(ModelMapperModelConfigurationAttribute)) return false;
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

        private static bool VersionChanged(this AssemblyDefinition assemblyDefinition){
            var versionAttribute = assemblyDefinition.CustomAttributes.First(attribute =>
                attribute.AttributeType.ToType() == typeof(AssemblyFileVersionAttribute));
            return Version.Parse(versionAttribute.ConstructorArguments.First().Value.ToString()) !=_modelMapperModuleVersion;
        }

        private static bool IsMapped(this AssemblyDefinition assemblyDefinition,(Type type, IModelMapperConfiguration configuration) data){
            var typeVersion = data.type.Assembly.GetName().Version;
            var modelMapperServiceAttributes = assemblyDefinition.CustomAttributes.Where(attribute => attribute.AttributeType.ToType() == typeof(ModelMapperServiceAttribute)).ToArray();
            return modelMapperServiceAttributes.Any(attribute => {
                var mappedTypeVersion = Version.Parse((string) attribute.ConstructorArguments.Last().Value);
                var mappedType = (string) attribute.ConstructorArguments.First().Value;
                return mappedTypeVersion == typeVersion && mappedType==data.type.FullName;

            });
        }

        private static IObservable<Type> Compile(this IObservable<(string code,IEnumerable<Assembly> references)> source){
            return source.SelectMany(_ => _.references.Compile(_.code).GetTypes().Where(type => typeof(IModelModelMap).IsAssignableFrom(type)));
        }
    }
}