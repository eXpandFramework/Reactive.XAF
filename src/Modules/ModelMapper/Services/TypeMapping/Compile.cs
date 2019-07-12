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
            
            var strings = references.ToArray();
            compilerParameters.ReferencedAssemblies.AddRange(strings);

            var compilerResults = codeProvider.CompileAssemblyFromSource(compilerParameters, code);
            if (compilerResults.Errors.Count > 0){
                var message = String.Join(Environment.NewLine,
                    compilerResults.Errors.Cast<CompilerError>().Select(error => error.ToString()));
                throw new Exception(message);
            }

            
            return RemoveRecursiveProperties(OutputAssembly);

        }

        private static Assembly RemoveRecursiveProperties(string assembly){
            
            using (var assemblyDefinition = AssemblyDefinition.ReadAssembly(assembly,new ReaderParameters(){ReadWrite = true})){
                var typeDefinitions = assemblyDefinition.MainModule.Types.Where(definition => definition.Interfaces.Any(_ => _.InterfaceType.FullName==typeof(IModelModelMap).FullName));
                foreach (var type in typeDefinitions){
                    assemblyDefinition.RemoveRecursiveProperties(type,type.FullName,type.FullName);    
                }
                assemblyDefinition.Write();
            }

            return Assembly.LoadFile(OutPutAssembly);
        }

        private static void RemoveRecursiveProperties(this AssemblyDefinition assemblyDefinition,TypeReference type,string chainTypes,string ch1){
            var recursiveCandinates = type.RecursiveCandinates();
            foreach (var propertyInfo in recursiveCandinates){
                var remove = type.Remove(chainTypes, propertyInfo);
                if (!remove){
                    chainTypes += $"/{propertyInfo.PropertyType.FullName}";
                    ch1 +=$"/{propertyInfo.PropertyType.FullName.Substring(propertyInfo.PropertyType.FullName.LastIndexOf("_", StringComparison.Ordinal))}";
                    assemblyDefinition.RemoveRecursiveProperties(propertyInfo.PropertyType,chainTypes,ch1);
                    chainTypes = string.Join("/", chainTypes.Split('/').SkipLast(1));
                    ch1 = string.Join("/", ch1.Split('/').SkipLast(1));
                }
            }
        }

        private static bool Remove(this TypeReference typeReference, string types,PropertyDefinition propertyDefinition){
            
            if (types.Split('/').Contains(propertyDefinition.PropertyType.FullName)){
                var typeDefinition = typeReference.Resolve();
                typeDefinition.Methods.Remove(propertyDefinition.GetMethod);
                typeDefinition.Methods.Remove(propertyDefinition.SetMethod);
                typeDefinition.Properties.Remove(propertyDefinition);
                return true;
            }

            return false;
        }

        private static PropertyDefinition[] RecursiveCandinates(this TypeReference type){
            return type.Resolve().Properties
                .Where(_ => !_.PropertyType.IsValueType && _.PropertyType.FullName!=typeof(string).FullName)
                .Where(definition => !definition.PropertyType.Resolve().Interfaces.Any(_ => _.InterfaceType.IsGenericInstance))
                .ToArray();
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
                var hashCode = HashCode(data);
                var typeMatch = ((string) attribute.ConstructorArguments.First().Value) == data.type.FullName;
                if (typeMatch){
                    return !attribute.ConstructorArguments.Last().Value.Equals(hashCode);
                }

                return false;
            });
            return configurationChanged;
        }

        private static int HashCode((Type type, IModelMapperConfiguration configuration) data){
            int hashCode = 0;
            if (data.configuration != null){
                hashCode = data.configuration.GetHashCode();
            }

            hashCode += string.Join("", PropertyMappingRules.Select(_ => _.key)
                    .Concat(TypeMappingRules.Select(_ => _.key))
                    .Concat(AdditionalTypesList.Select(_ => _.FullName))
                    .Concat(ReservedPropertyNames)
                    .Concat(ReservedPropertyInstances.Select(_ => _.FullName))
                    .Concat(new[]{ModelMappersNodeName, MapperAssemblyName, ModelMapperAssemblyName, DefaultContainerSuffix}))
                .GetHashCode();
            return $"{hashCode}".GetHashCode();
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