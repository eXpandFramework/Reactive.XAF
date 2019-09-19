using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CSharp;
using Mono.Cecil;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.Reactive.Extensions;


namespace Xpand.XAF.Modules.ModelMapper.Services.TypeMapping{
    public static partial class TypeMappingService{
        


        private static IObservable<Assembly> Compile(this IEnumerable<string> references, string code){
            
            var codeProvider = new CSharpCodeProvider();
            var compilerParameters = new CompilerParameters{
                CompilerOptions = "/t:library /optimize",
                OutputAssembly = _outputAssembly
            };
            
            var strings = references.ToArray();
            compilerParameters.ReferencedAssemblies.AddRange(strings);

            var compilerResults = codeProvider.CompileAssemblyFromSource(compilerParameters, code);
            if (compilerResults.Errors.Count > 0){
                var message = String.Join(Environment.NewLine,
                    compilerResults.Errors.Cast<CompilerError>().Select(error => error.ToString()));
                throw new Exception(message);
            }

            var assembly = RemoveRecursiveProperties(_outputAssembly);
            return assembly.AsObservable().TraceModelMapper();

        }

        private static Assembly RemoveRecursiveProperties(string assembly){
            
            using (var assemblyDefinition = AssemblyDefinition.ReadAssembly(assembly,new ReaderParameters(){ReadWrite = true})){
                var typeDefinitions = assemblyDefinition.MainModule.Types.Where(definition => definition.Interfaces.Any(_ => _.InterfaceType.FullName==typeof(IModelModelMap).FullName));
                foreach (var type in typeDefinitions){
                    assemblyDefinition.RemoveRecursiveProperties(type,type.FullName);    
                }
                assemblyDefinition.Write();
            }

            return Assembly.LoadFile(assembly);
        }

        private static void RemoveRecursiveProperties(this AssemblyDefinition assemblyDefinition,TypeReference type,string chainTypes){
            var recursiveCandinates = type.RecursiveCandinates();
            foreach (var propertyInfo in recursiveCandinates){
                var remove = type.Remove(chainTypes, propertyInfo);
                if (!remove){
                    chainTypes += $"/{propertyInfo.PropertyType.FullName}";
                    assemblyDefinition.RemoveRecursiveProperties(propertyInfo.PropertyType,chainTypes);
                    chainTypes = string.Join("/", chainTypes.Split('/').SkipLast(1));
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

        private static bool TypeFromPath(this IModelMapperConfiguration configuration){
            var assemblyPath = GetLastAssemblyPath();
            if (assemblyPath!=null){
                using (var assembly = AssemblyDefinition.ReadAssembly(assemblyPath)){
                    if (assembly.IsMapped(configuration) && !assembly.VersionChanged() && !assembly.ConfigurationChanged()){
                        return true;
                    }
                }
            }
            return false;
        }

        private static string GetLastAssemblyPath(){
            var assemblyPath = Directory
                .GetFiles($"{Path.GetDirectoryName(_outputAssembly)}", $"{_outPutAssemblyNamePattern}*.dll")
                .OrderByDescending(s => s).FirstOrDefault();
            return assemblyPath;
        }

        private static bool ConfigurationChanged(this AssemblyDefinition assembly){
            var configurationChanged = assembly.CustomAttributes.Any(attribute => {
                if (attribute.AttributeType.FullName != typeof(ModelMapperServiceAttribute).FullName) return false;
                var hashCode = HashCode();
                var storedHash = attribute.ConstructorArguments.Last().Value;
                return !storedHash.Equals(hashCode);
            });
            return configurationChanged;
        }

        private static int HashCode(){
            var text = string.Join(Environment.NewLine, PropertyMappingRules.Select(_ => _.key)
                .Concat(TypeMappingRules.Select(_ => _.key))
                .Concat(AdditionalTypesList.Select(_ => _.FullName))
                .Concat(ReservedPropertyTypes.Select(type => type.FullName))
                .Concat(ReservedPropertyNames)
                .Concat(ReservedPropertyInstances.Select(_ => _.FullName))
                .Concat(new[]{ModelMappersNodeName, MapperAssemblyName, ModelMapperAssemblyName, DefaultContainerSuffix}).OrderBy(s => s));
            var hashCode = text
                .GetHashCode();
            return hashCode;
        }

        private static bool VersionChanged(this AssemblyDefinition assembly){
            var versionAttribute = assembly.CustomAttributes.First(attribute =>
                attribute.AttributeType.FullName == typeof(AssemblyFileVersionAttribute).FullName);
            return Version.Parse(versionAttribute.ConstructorArguments.First().Value.ToString()) !=_modelMapperModuleVersion;
        }

        private static bool IsMapped(this AssemblyDefinition assembly,IModelMapperConfiguration configuration){
            var typeAssemblyHash = configuration.TypeToMap.Assembly.ManifestModule.ModuleVersionId.GetHashCode();
            var modelMapperServiceAttributes = assembly.CustomAttributes.Where(attribute => attribute.AttributeType.FullName == typeof(ModelMapperTypeAttribute).FullName).ToArray();
            return modelMapperServiceAttributes.Any(attribute => {
                var mappedTypeAssemblyHash = int.Parse(attribute.ConstructorArguments.Skip(2).First().Value.ToString());
                var mappedType = (string) attribute.ConstructorArguments.First().Value;
                return mappedTypeAssemblyHash == typeAssemblyHash && mappedType == configuration.TypeToMap.FullName &&
                       configuration.GetHashCode() == (int) attribute.ConstructorArguments.Last().Value;

            });
        }

    }
}