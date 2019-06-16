using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text.RegularExpressions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Fasterflect;
using Microsoft.CSharp;
using Mono.Cecil;
using Xpand.Source.Extensions.Linq;
using Xpand.Source.Extensions.MonoCecil;
using Xpand.Source.Extensions.System.Refelction;
using Xpand.Source.Extensions.System.String;
using Xpand.Source.Extensions.XAF.XafApplication;

namespace Xpand.XAF.Modules.ModelMapper{
    public class ModelMapperConfiguration:IModelMapperConfiguration{
        public string ContainerName{ get; set; }
        public string MapName{ get; set; }
        public string ImageName{ get; set; }
        public string VisibilityCriteria{ get; set; }
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode(){
            return $"{ContainerName}{MapName}{ImageName}{VisibilityCriteria}".GetHashCode();
        }
    }
    public interface IModelMapperConfiguration{
        string VisibilityCriteria{ get;  }
        string ContainerName{ get; }
        string MapName{ get; }
        string ImageName{ get; }
    }

    [AttributeUsage(AttributeTargets.Assembly,AllowMultiple = true)]
    public class ModelMapperModelConfigurationAttribute:Attribute{
        public ModelMapperModelConfigurationAttribute(string typeName,int hashCode){
            TypeName = typeName;
            HashCode = hashCode;
        }

        public string TypeName{ get; }
        public int HashCode{ get; }
    }

    [AttributeUsage(AttributeTargets.Assembly,AllowMultiple = true)]
    public class ModelMapperServiceAttribute:Attribute{
        public ModelMapperServiceAttribute(string mappedType,string mappedAssemmbly, string version){
            MappedAssemmbly = mappedAssemmbly;
            MappedType = mappedType;
            Version = Version.Parse(version);
        }

        public string MappedAssemmbly{ get; }

        public Version Version{ get; }
        public string MappedType{ get;  }
    }

    public static class ModelMapperService{
        public static string DefaultContainerSuffix="Map";
        public static string ModelMapperAssemblyName=null;
        public static string MapperAssemblyName="ModelMapperAssembly";
        public static string ModelMappersNodeName="ModelMappers";
        private static Platform _platform;
        private static Version _modelMapperModuleVersion;
        public static List<string> ReservedPropertyNames{ get; }=new List<string>();
        public static List<Type> ReservedPropertyTypes{ get; }=new List<Type>();
        public static List<Type> ReservedAttributeTypes{ get; }=new List<Type>();
        public static Dictionary<Type,(Type type,Func<Attribute,object> result)> AttributesMap{ get; }=new Dictionary<Type, (Type, Func<Attribute, object>)>();
        static ISubject<(Type type,IModelMapperConfiguration configuration)> _typesToMap;
        private static string _outputAssembly;

        static ModelMapperService(){
            Init();
        }

        public static void Connect(){
            _typesToMap.OnCompleted();
        }

        private static void Init(){
            _typesToMap = Subject.Synchronize(new ReplaySubject<(Type type,IModelMapperConfiguration configuration)>());
            MappedTypes = Observable.Defer(() => {
                var distinnctTypesToMap = Observable.Defer(() => _typesToMap.Distinct(_ => $"{_.type.AssemblyQualifiedName}{_.configuration?.GetHashCode()}"));
                return distinnctTypesToMap
                    .All(_ => _.TypeFromPath())
                    .Select(_ =>!_? distinnctTypesToMap.GenerateCode().Compile(): Assembly.LoadFile(_outputAssembly).GetTypes()
                                .Where(type => typeof(IModelModelMap).IsAssignableFrom(type)).ToObservable()).Switch();
            }).Replay().AutoConnect();
            _modelMapperModuleVersion = typeof(ModelMapperService).Assembly.GetName().Version;
            _platform = XafApplicationExtensions.ApplicationPlatform;
            ReservedPropertyNames.Clear();
            ReservedPropertyNames.AddRange(typeof(IModelNode).Properties().Select(info => info.Name));
            ReservedPropertyTypes.AddRange(new[]{ typeof(Type)});
            ReservedAttributeTypes.Add(typeof(DefaultValueAttribute));
            AttributesMap.Clear();
            ModelMapperExtenderService.Init();
            _outputAssembly = $@"{Path.GetDirectoryName(typeof(ModelMapperService).Assembly.Location)}\{ModelMapperAssemblyName}{MapperAssemblyName}{_platform}.dll";
        }

        public static IObservable<Type> Compile(this IObservable<((Type type,IModelMapperConfiguration configuration)[] typeData, (string code, IEnumerable<Assembly> references)? codeData)> source){
            return source.SelectMany(_ => {
                if (_.codeData.HasValue){
                    var assembly = _.codeData.Value.references.Compile(_.codeData.Value.code);
                    return _.typeData.Select(data => assembly.GetType($"IModel{data.type.ModelMapName(data.configuration)}"));
                }

                return _.typeData.Select(data => data.type);
            });
        }


        static IObservable<((Type type, IModelMapperConfiguration configuration)[] typeData, (string code,IEnumerable<Assembly> references)? codeData)> 
            GenerateCode(this IObservable<(Type type, IModelMapperConfiguration configuration)> source){

            return source.Select(_ => (types:new[]{(_.type, _.configuration)}, codeData:_.type.GenerateCode(_.configuration)))
                .Aggregate((acc, cu) => {
                    if (acc.codeData != null && cu.codeData != null){
                        var references = acc.codeData.Value.references.Concat(cu.codeData.Value.references).ToArray();
                        var types = acc.types.Concat(cu.types).ToArray();
                        var code = cu.codeData.Value.code;
                        var assemblyAttributes=string.Join(Environment.NewLine,Regex.Matches(code, @"\[assembly:[^\]]*\]").Cast<Match>().Select(_ => _.Value));
                        assemblyAttributes=Regex.Replace(assemblyAttributes, $@"\[assembly:{typeof(AssemblyVersionAttribute).FullName}[^\]]*\]", "");
                        assemblyAttributes=Regex.Replace(assemblyAttributes, $@"\[assembly:{typeof(AssemblyFileVersionAttribute).FullName}[^\]]*\]", "");
                        code=Regex.Replace(code, @"\[assembly:[^\]]*\]", "");
                        var accCode = $"{assemblyAttributes}{Environment.NewLine}{acc.codeData.Value.code}{Environment.NewLine}{code}";
                        return (types, (accCode, references));
                    }

                    return (cu.types, null);
                });
        }

        public static IObservable<Type> MappedTypes{ get;private set; }

        public static IObservable<Type> MapToModel<TModelMapperConfiguration>(this (Type type, TModelMapperConfiguration configuration) type)
            where TModelMapperConfiguration : IModelMapperConfiguration{

            return new[]{type}.MapToModel();
        }

        public static IObservable<Type> MapToModel(this IEnumerable<Type> types){
            return types.Select(_ => (_, new ModelMapperConfiguration())).ToArray().MapToModel();
        }

        public static IObservable<Type> ModelInterfaces(this IObservable<Type> source){
            return source.Finally(Connect).Select(_ => MappedTypes).Switch();
        }

        public static IObservable<Type> MapToModel(this Type type,IModelMapperConfiguration configuration=null){
            return new []{(type,configuration)}.MapToModel();
        }

        public static IObservable<Type> MapToModel<TModelMapperConfiguration>(this (Type type,TModelMapperConfiguration configuration)[] types) where TModelMapperConfiguration:IModelMapperConfiguration{
            return types
                .Select(_ => (_.type,(IModelMapperConfiguration)_.configuration)).ToObservable()
                .Do(_ => _typesToMap.OnNext(_))
                .Select(_ => _.type);
        }

        public static IObservable<Type> MapToModel<TModelMapperConfiguration>(this XafApplication application, params (Type type,TModelMapperConfiguration configuration)[] types) where TModelMapperConfiguration:IModelMapperConfiguration{
            return types.MapToModel();
        }

        public static IObservable<Type> MapToModel(this ModuleBase moduleBase, params (Type type,IModelMapperConfiguration configuration)[] types){
            return types.MapToModel();
        }

        static (string code,IEnumerable<Assembly> references)? GenerateCode(this Type type,IModelMapperConfiguration configuration=null){
            var propertyInfos = type.PropertyInfos();
            var additionalTypes = propertyInfos.AdditionalTypes(type);
            var assemblyDefinitions = type.AssemblyDefinitions( additionalTypes);

            var additionalTypesCode = type.AdditionalTypesCode( additionalTypes, assemblyDefinitions);

            var containerName = type.ModelMapContainerName( configuration);
            var mapName = type.ModelMapName( configuration);
            var containerCode = type.ContainerCode( configuration, $"IModel{containerName}", assemblyDefinitions, mapName);

            var modelMappersTypeName = $"IModel{containerName}{ModelMappersNodeName}";
            var modelMappersInterfaceCode = ModelMappersInterfaceCode( modelMappersTypeName);

            var typeCode = type.TypeCode(mapName, modelMappersTypeName, assemblyDefinitions,configuration?.ImageName);

            foreach (var assemblyDefinition in assemblyDefinitions){
                assemblyDefinition.Dispose();
            }

            var code = String.Join(Environment.NewLine,
                new[] {type.AssemblyAttributesCode(configuration), typeCode, containerCode, modelMappersInterfaceCode}.Concat(additionalTypesCode));

            var allAssemblies = AllAssemblies(type, propertyInfos);
            return (code,allAssemblies);
        }

        public static string ModelMapContainerName(this Type type, IModelMapperConfiguration configuration=null){
            return configuration?.ContainerName?? $"{type.Name}{DefaultContainerSuffix}";
        }

        public static string ModelMapName(this Type type, IModelMapperConfiguration configuration=null){
            return configuration?.MapName??type.Name;
        }

        private static bool TypeFromPath(this (Type type,IModelMapperConfiguration configuration) data){
            if (File.Exists(_outputAssembly)){
                using (var assemblyDefinition = AssemblyDefinition.ReadAssembly(_outputAssembly)){
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

        private static IEnumerable<Assembly> AllAssemblies(Type type, PropertyInfo[] propertyInfos){
            return propertyInfos.Select(info => info.PropertyType)
                .Concat(new[]{type,typeof(DescriptionAttribute),typeof(ModelMapperServiceAttribute)})
                .Concat(propertyInfos.SelectMany(info =>
                    info.GetCustomAttributes(typeof(Attribute), false).Select(o => o.GetType())))
                .Concat(AttributesMap.Keys).Concat(AttributesMap.Values.Select(_ => _.type))
                .Select(_ => _.Assembly)
                .Distinct();
        }

        private static IEnumerable<string> AdditionalTypesCode(this Type type, Type[] additionalTypes,
            AssemblyDefinition[] assemblyDefinitions){
            var mappedTypes = new HashSet<Type>(new[]{type});
            var additionalTypesCode = additionalTypes.Where(_ => !mappedTypes.Contains(_))
                .Select(_ => {
                    var modelCode = _.ModelCode(assemblyDefinitions, mappedTypes: mappedTypes);
                    mappedTypes.Add(_);
                    return modelCode;
                });
            return additionalTypesCode;
        }


        private static string TypeCode(this Type type,string mapName, string modelMappersTypeName, AssemblyDefinition[] assemblyDefinitions,string imageName){
            var domainLogic = $@"[{typeof(DomainLogicAttribute).FullName}(typeof({modelMappersTypeName}))]public class {modelMappersTypeName}DomainLogic{{public static int? Get_Index({modelMappersTypeName} mapper){{return 0;}}}}{Environment.NewLine}";
            string modelMappersPropertyCode = $"new int? Index{{get;set;}}{Environment.NewLine}{modelMappersTypeName} {ModelMappersNodeName} {{get;}}";
            var typeCode = type.ModelCode(assemblyDefinitions,imageName,mapName, additionalPropertiesCode: modelMappersPropertyCode,
                baseType: typeof(IModelModelMap));
            return $"{domainLogic}{typeCode}";
        }

        private static string ModelMappersInterfaceCode(string modelMappersTypeName){
            string modelMapperContextContainerName=typeof(IModelMapperContextContainer).FullName;
            var nodesGeneratorName=typeof(ModelMapperContextNodeGenerator).FullName;
            var imageCode=$@"[{typeof(ImageNameAttribute).FullName}(""{ModelImageSource.ModelModelMapperContexts}"")]{Environment.NewLine}";
            string modelGeneratorCode=$"[{typeof(ModelNodesGeneratorAttribute)}(typeof({nodesGeneratorName}))]{Environment.NewLine}";
            var descriptionCode=$@"[{typeof(DescriptionAttribute)}(""These mappers relate to Application.ModelMapper.MapperContexts and applied first."")]{Environment.NewLine}";
            var modelMappersInterfaceCode =
                $@"{descriptionCode}{modelGeneratorCode}{imageCode}public interface {modelMappersTypeName}:{typeof(IModelList).FullName}<{modelMapperContextContainerName}>,{typeof(IModelNode).FullName}{{}}";
            return modelMappersInterfaceCode;
        }

        private static AssemblyDefinition[] AssemblyDefinitions(this Type type, Type[] types){
            return types
                .Select(_ => _.Assembly)
                .Concat(new[]{type}.Select(_ => _.Assembly))
                .Distinct()
                .Select(assembly => AssemblyDefinition.ReadAssembly(assembly.Location))
                .ToArray();
        }

        private static Type[] AdditionalTypes(this PropertyInfo[] propertyInfos,Type type){
            return propertyInfos
                .Where(_ => !_.PropertyType.IsValueType && typeof(string) != _.PropertyType && _.PropertyType != type)
                .Select(_ => _.PropertyType)
                .DistinctBy(_ => _.ModelName())
                .ToArray();
        }

        private static PropertyInfo[] PropertyInfos(this Type type){
            return type.PublicProperties()
                .GetItems<PropertyInfo>(_ => _.PropertyType.PublicProperties(), info => info.PropertyType)
                .ToArray();
        }
        
        private static string AssemblyAttributesCode(this Type type,IModelMapperConfiguration configuration){
            var modelMapperServiceAttributeCode = ModelMapperServiceAttributeCode(type);
            var assemblyVersionCode = $@"[assembly:{typeof(AssemblyVersionAttribute).FullName}(""{_modelMapperModuleVersion}"")]{Environment.NewLine}[assembly:{typeof(AssemblyFileVersionAttribute).FullName}(""{_modelMapperModuleVersion}"")]";
            int hashCode = 0;
            if (configuration != null) hashCode = configuration.GetHashCode();
            var modelMapperConfigurationCode = $@"[assembly:{typeof(ModelMapperModelConfigurationAttribute).FullName}(""{type.FullName}"",{hashCode})]{Environment.NewLine}";
            return string.Join(Environment.NewLine, modelMapperConfigurationCode, assemblyVersionCode, modelMapperServiceAttributeCode);
        }

        private static string ModelMapperServiceAttributeCode(this Type type){
            return $@"[assembly:{typeof(ModelMapperServiceAttribute).FullName}(""{type.FullName}"",""{type.Assembly.GetName().Name}"",""{type.Assembly.GetName().Version}"")]";
        }

        private static string ContainerCode(this Type type, IModelMapperConfiguration configuration, string modelName,
            AssemblyDefinition[] assemblyDefinitions, string mapName){
            var modelBrowseableCode = configuration.ModelBrowseableCode();
            return type.ModelCode(assemblyDefinitions, configuration?.ImageName, $"{modelName}".Substring(6),
                $"{modelBrowseableCode}IModel{mapName} {mapName}{{get;}}",baseType:typeof(IModelModelMapContainer));
        }

        private static string ModelBrowseableCode(this IModelMapperConfiguration configuration){
            var visibilityCriteria = configuration?.VisibilityCriteria;
            visibilityCriteria = visibilityCriteria == null ? "null" : $@"""{visibilityCriteria}""";
            var browseableCode =
                $"[{typeof(ModelMapperBrowsableAttribute).FullName}(typeof({typeof(ModelMapperVisibilityCalculator).FullName}),{visibilityCriteria})]{Environment.NewLine}";
            return browseableCode;
        }

        private static string ModelCode(this Type type, AssemblyDefinition[] assemblyDefinitions,string imageName=null, string customName = null,string propertiesCode = null,string additionalPropertiesCode=null,Type baseType=null,HashSet<Type> mappedTypes=null){
            mappedTypes = mappedTypes ?? new HashSet<Type>();
            baseType = baseType ?? typeof(IModelNodeDisabled);
            var assemblyName = type.Assembly.GetName().Name;
            var assemblyDefinition = assemblyDefinitions.First(definition => definition.Name.Name==assemblyName);
            var typeFullName = $"{type.FullName}";
            TypeDefinition typeDefinition;
            if (type.FullName?.IndexOf("+", StringComparison.Ordinal) > -1){
                typeFullName = typeFullName.Replace("+", "/");
                typeDefinition = assemblyDefinition.MainModule.Types.SelectMany(definition => definition.NestedTypes).First(definition => definition.FullName==typeFullName);
            }
            else{
                typeDefinition=assemblyDefinition.MainModule.Types.First(definition => definition.FullName == typeFullName);
            }
            var properties = type.PublicProperties().Where(info => !mappedTypes.Contains(info.PropertyType)).ToArray();
            propertiesCode = propertiesCode ?? properties.ModelCode(typeDefinition);
            propertiesCode += $"{Environment.NewLine}{additionalPropertiesCode}";
            string imageCode = null;
            if (imageName!=null){
                imageCode = $@"[{typeof(ImageNameAttribute).FullName}(""{imageName}"")]{Environment.NewLine}";
            }
            return $"{imageCode}public interface {type.ModelName(customName)}:{baseType.FullName}{{{Environment.NewLine}{propertiesCode}{Environment.NewLine}}}";
        }

        private static string ModelName(this Type type,string customName=null){
            return $"IModel{customName??type.FullName.CleanCodeName()}";
        }

        private static IEnumerable<PropertyInfo> PublicProperties(this Type type){
            return type.Properties(Flags.AllMembers)
                .Where(info => {
                    if (info.PropertyType.IsValueType || info.PropertyType == typeof(string)){
                        return info.CanRead && info.CanWrite;
                    }

                    return true;
                })
                .Where(info => info.AccessModifier()==AccessModifier.Public&& !ReservedPropertyNames.Contains(info.Name)&&!typeof(ICollection).IsAssignableFrom(info.PropertyType))
                .Where(info => {
                    if (info.PropertyType == typeof(string) || info.PropertyType.IsNullableType()) return true;
                    return !info.PropertyType.IsGenericType && info.PropertyType != type &&
                           info.PropertyType != typeof(object) && ReservedPropertyTypes.Any(_ => info.PropertyType!=_);
                })
                .DistinctBy(info => info.Name);
        }

        static string AttributeCtorArguments(this CustomAttribute attribute){
            var ctorArguments = string.Join(",", attribute.ConstructorArguments.Select(argument => {
                if (argument.Value == null){
                    return "null";
                }
                if (argument.Type.FullName == typeof(string).FullName && argument.Value != null){
                    return $@"""{argument.Value}""";
                }
                if (argument.Type.FullName == typeof(Type).FullName){
                    return $"typeof({argument.Value})";
                }
                if (argument.Type.FullName == typeof(bool).FullName){
                    return argument.Value?.ToString().ToLower();
                }
                if (argument.Value is CustomAttributeArgument customAttributeArgument){
                    var typeReference = customAttributeArgument.Type;
                    var value = customAttributeArgument.Value;
                    return typeReference.Resolve().IsEnum ? GetEnums(typeReference, value) : value ?? "null";
                }
                var resolvedType = argument.Type.Resolve();
                return resolvedType.IsEnum ? GetEnums(argument.Type, argument.Value) : argument.Value;
            }));
            return ctorArguments;
        }

        private static object GetEnums(TypeReference typeReference, object value){
            var enumType = typeReference.ToType();
            if (EnumsNET.NonGeneric.NonGenericFlagEnums.IsFlagEnum(enumType)&&EnumsNET.NonGeneric.NonGenericFlagEnums.HasAnyFlags(enumType,value)){
                return string.Join("|", EnumsNET.NonGeneric.NonGenericFlagEnums.GetFlagMembers(enumType, value)
                    .Select(member => $"{enumType.FullName}.{member.Name}"));
            }

            var name = Enum.GetName(enumType, value);
            return $"{enumType.FullName}.{name}";
        }

        static string ModelCode(this IEnumerable<Attribute> attributes,CustomAttribute[] customAttributes){
            var modelCode = string.Join(Environment.NewLine, attributes
                .Where(CanBeMapped)
                .Select(attribute => {
                    var attributeType = attribute.GetType();
                    var descriptionAttributeType = typeof(DescriptionAttribute);
                    if (descriptionAttributeType.IsAssignableFrom(attributeType)){
                        return $@"[{descriptionAttributeType}(""{((DescriptionAttribute) attribute).Description.Replace(@"""",@"")}"")]";
                    }
                    var customAttribute =customAttributes.First(_ => _.AttributeType.FullName == attributeType.FullName);
                    var name = attributeType.FullName;
                    if (AttributesMap.ContainsKey(attributeType)){
                        var map = AttributesMap[attributeType];
                        name = map.type.FullName;
                        return $"[{name}({map.result(attribute)})]";
                    }
                    return AllArgsAreValid(customAttribute) ? $"[{name}({customAttribute.AttributeCtorArguments()})]" : null;
                }));
            return modelCode;
        }

        private static bool CanBeMapped(Attribute attribute){
            return attribute is DescriptionAttribute || AttributesMap.ContainsKey(attribute.GetType()) ||
                   attribute.GetType().IsPublic && !ReservedAttributeTypes.Contains(attribute.GetType());
        }

        private static bool AllArgsAreValid(CustomAttribute customAttribute){
            var allArgsAreValid = customAttribute.ConstructorArguments
                .All(argument => {
                    var isvalid = (!(argument.Value is TypeDefinition typeDefinition) ||typeDefinition.IsPublic && !typeDefinition.IsFlag());
                    if (isvalid){
                        if (argument.Value is CustomAttributeArgument customAttributeArgument){
                            var definition = customAttributeArgument.Type.Resolve();
                            var isPublic = definition.IsPublic;
                            return isPublic && (!definition.IsFlag() ||!EnumsNET.NonGeneric.NonGenericFlagEnums.HasAnyFlags(
                                                    definition.ToType(),customAttributeArgument.Value));
                        }
                        return true;
                    }
                    return false;
                });
            return allArgsAreValid;
        }

        private static bool IsFlag(this TypeDefinition typeDefinition){
            return (typeDefinition.IsEnum&&typeDefinition.CustomAttributes.Any(attribute1 => attribute1.AttributeType.FullName==typeof(FlagsAttribute).FullName));
        }

        private static string ModelCode(this IEnumerable<PropertyInfo> properties, TypeDefinition typeDefinition){
            return String.Join(Environment.NewLine,properties
                .Select(info => {
                    string propertyCode=null;
                    if (info.PropertyType.IsValueType||info.PropertyType == typeof(string)){
                        if (info.CanRead && info.CanWrite){
                            string nullSign=null;
                            var infoPropertyType = info.PropertyType.ToString();
                            var isNullAble = info.PropertyType.IsGenericType && info.PropertyType.GetGenericTypeDefinition() ==typeof(Nullable<>);
                            if (info.PropertyType.IsValueType ){
                                nullSign = "?";
                            }
                            if (isNullAble){
                                infoPropertyType = info.PropertyType.GenericTypeArguments.First().ToString();
                            }
                            propertyCode = $"{infoPropertyType.Replace("+",".")}{nullSign} {info.Name}{{get;set;}}";
                        }
                    }
                    else{
                        propertyCode=$"{info.PropertyType.ModelName()} {info.Name}{{get;}}";
                    }

                    var customAttributes = typeDefinition.Properties
                        .Where(definition => definition.Name==info.Name)
                        .SelectMany(definition => definition.CustomAttributes)
                        .Concat(typeDefinition.BaseClasses()
                            .SelectMany(definition => definition.Properties.Where(definition1 => definition1.Name==info.Name)
                                .SelectMany(definition1 => definition1.CustomAttributes))).ToArray();

                    if (propertyCode != null){
                        var attributesCode=info.GetCustomAttributes(typeof(Attribute),false).Cast<Attribute>().ModelCode(customAttributes);
                        return $"{attributesCode}\r\n{propertyCode}";    
                    }

                    return null;
                }));
        }


        private static Assembly Compile(this IEnumerable<Assembly> references, string code){
            var codeProvider = new CSharpCodeProvider();
            var compilerParameters = new CompilerParameters{
                CompilerOptions = "/t:library",
                OutputAssembly = _outputAssembly
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
    }
}