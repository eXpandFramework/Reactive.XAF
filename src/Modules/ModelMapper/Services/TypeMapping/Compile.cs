using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Mono.Cecil;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.BytesExtensions;
using Xpand.Extensions.Compiler;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.StreamExtensions;
using Xpand.Extensions.StringExtensions;
using Xpand.XAF.Modules.ModelMapper.Configuration;


namespace Xpand.XAF.Modules.ModelMapper.Services.TypeMapping{
	
    public static partial class TypeMappingService{
        public static string CustomAssemblyNameSuffix = "Custom";
        private static bool _skipAssemblyValidation;
        static readonly Subject<GenericEventArgs<(string code,IEnumerable<string> references,string outputAssembly)>> CustomCompileSubject=new Subject<GenericEventArgs<(string code, IEnumerable<string> references, string outputAssembly)>>();

        public static IObservable<GenericEventArgs<(string code, IEnumerable<string> references, string outputAssembly)>> CustomCompile1 => CustomCompileSubject.AsObservable();

        private static IObservable<Assembly> Compile(this IEnumerable<string> references, string code,bool isCustom){
	        
	        var outputAssembly = FormatOutputAssembly(isCustom);
	        if (File.Exists(outputAssembly)){
                File.Delete(outputAssembly);
	        }

            var args = new GenericEventArgs<(string code, IEnumerable<string> references,string outputAssembly)>();
            CustomCompileSubject.OnNext(args);
            if (!args.Handled) {
                using var memoryStream = CSharpSyntaxTree.ParseText(code).Compile(references.ToArray());
                memoryStream.Bytes().Save(outputAssembly);
            }
            var assembly = RemoveRecursiveProperties(outputAssembly);
            return assembly.ReturnObservable().TraceModelMapper();
        }

        private static string FormatOutputAssembly(bool isCustom) {
            var outputAssembly = string.Format(OutputAssembly, ModelExtendingService.Platform);
            return isCustom?$"{Path.Combine(Path.GetDirectoryName(outputAssembly)!,$"{Path.GetFileNameWithoutExtension(outputAssembly)}{CustomAssemblyNameSuffix}.dll")}":outputAssembly;
        }

        private static Assembly RemoveRecursiveProperties(string assembly){
	        using (var assemblyDefinition = AssemblyDefinition.ReadAssembly(assembly,new ReaderParameters(){ReadWrite = true})){
		        assemblyDefinition.Name = new AssemblyNameDefinition(Path.GetFileNameWithoutExtension(assembly), assemblyDefinition.Name.Version);
                var typeDefinitions = assemblyDefinition.MainModule.Types.Where(definition => definition.Interfaces.Any(_ => _.InterfaceType.FullName==typeof(IModelModelMap).FullName));
                foreach (var type in typeDefinitions){
                    assemblyDefinition.RemoveRecursiveProperties(type,type.FullName);    
                }
                assemblyDefinition.Write();
            }
            return AppDomain.CurrentDomain.LoadAssembly(assembly);
        }

        private static void RemoveRecursiveProperties(this AssemblyDefinition assemblyDefinition,TypeReference type,string chainTypes){
            foreach (var propertyInfo in type.RecursiveCandidates()){
                var remove = type.Remove(chainTypes, propertyInfo);
                if (!remove){
                    chainTypes += $"/{propertyInfo.PropertyType.FullName}";
                    assemblyDefinition.RemoveRecursiveProperties(propertyInfo.PropertyType,chainTypes);
                    chainTypes = string.Join("/", chainTypes.Split('/').SkipLastN(1));
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

        private static PropertyDefinition[] RecursiveCandidates(this TypeReference type) 
            => type.Resolve().Properties
                .Where(_ => !_.PropertyType.IsValueType && _.PropertyType.FullName!=typeof(string).FullName)
                .Where(definition => !definition.PropertyType.Resolve().Interfaces.Any(_ => _.InterfaceType.IsGenericInstance))
                .ToArray();

        private static bool TypeFromPath(this IModelMapperConfiguration configuration,bool isCustom){
            var assemblyPath = GetLastAssemblyPath(isCustom);
            if (!string.IsNullOrEmpty(assemblyPath)){
                using var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
                if (!assembly.VersionChanged()) {
                    return !isCustom || assembly.IsMapped(configuration) && !assembly.ConfigurationChanged();
                }
            }
            return false;
        }

        private static string GetLastAssemblyPath(bool isCustom){
            var outputAssembly = FormatOutputAssembly(isCustom);
            return Directory
                .GetFiles($"{Path.GetDirectoryName(outputAssembly)}", $"{Path.GetFileNameWithoutExtension(outputAssembly)}*.dll")
                .OrderByDescending(s => s)
                .FirstOrDefault(s => {
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(s);
                    return !isCustom ? !fileNameWithoutExtension.EndsWith(CustomAssemblyNameSuffix)
                        : fileNameWithoutExtension.EndsWith(CustomAssemblyNameSuffix);
                });
        }

        private static bool ConfigurationChanged(this AssemblyDefinition assembly) 
            => assembly.CustomAttributes.Any(attribute => {
                if (attribute.AttributeType.FullName != typeof(ModelMapperServiceAttribute).FullName) return false;
                var hashCode = HashCode();
                var storedHash = attribute.ConstructorArguments.Last().Value;
                return !storedHash.Equals(hashCode);
            });

        private static string HashCode() 
            => string.Join(Environment.NewLine, PropertyMappingRules.Select(_ => _.key)
                    .Concat(TypeMappingRules.Select(_ => _.key))
                    .Concat(AdditionalTypesList.Select(_ => _.FullName))
                    .Concat(ReservedPropertyTypes.Select(type => type.FullName))
                    .Concat(ReservedPropertyNames)
                    .Concat(ReservedPropertyInstances.Select(_ => _.FullName))
                    .Concat(new[]{ModelMappersNodeName, MapperAssemblyName, ModelMapperAssemblyName, DefaultContainerSuffix}).OrderBy(s => s))
                .ToGuid().ToString();

        private static bool VersionChanged(this AssemblyDefinition assembly) 
            => Version.Parse(assembly.CustomAttributes
                .First(attribute => attribute.AttributeType.FullName == typeof(AssemblyFileVersionAttribute).FullName).ConstructorArguments
                .First().Value.ToString()) !=_modelMapperModuleVersion;

        private static bool IsMapped(this AssemblyDefinition assembly, IModelMapperConfiguration configuration){
            var typeAssemblyHash = configuration.TypeToMap.Assembly.ManifestModule.ModuleVersionId.ToString();
            var modelMapperServiceAttributes = assembly.CustomAttributes.Where(attribute => attribute.AttributeType.FullName == typeof(ModelMapperTypeAttribute).FullName).ToArray();
            return modelMapperServiceAttributes.Any(attribute => {
                var mappedTypeAssemblyHash = attribute.ConstructorArguments.Skip(2).First().Value.ToString();
                var mappedType = (string) attribute.ConstructorArguments.First().Value;
                return mappedTypeAssemblyHash == typeAssemblyHash && mappedType == configuration.TypeToMap.FullName &&
                       configuration.ToString().ToGuid().ToString() ==  (string) attribute.ConstructorArguments.Last().Value;

            });
        }

    }
}