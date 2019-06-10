using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Fasterflect;
using Microsoft.CSharp;
using Mono.Cecil;
using Xpand.Source.Extensions.Linq;
using Xpand.Source.Extensions.MonoCecil;
using Xpand.Source.Extensions.System.AppDomain;
using Xpand.Source.Extensions.System.Refelction;
using Xpand.Source.Extensions.System.String;
using Xpand.Source.Extensions.XAF.XafApplication;

namespace Xpand.XAF.Modules.ModelMapper{
    public class ModelMapperConfiguration:IModelMapperConfiguration{
        public string CustomContainerName{ get; set; }
        public string CustomContainerPersistentName{ get; set; }
        public string ImageName{ get; set; }
        public string VisibilityCriteria{ get; set; }
    }
    public interface IModelMapperConfiguration{
        string VisibilityCriteria{ get;  }
        string CustomContainerName{ get; }
        string CustomContainerPersistentName{ get; }
        string ImageName{ get; }
    }
    public static class ModelMapperService{
        public static string ModelMapperAssemblyName=null;
        public static string MapperAssemblyName="ModelMapperAssembly";
        public static string ModelMappersNodeName="ModelMappers";
        public static string ContainerSuffix="Container";
        private static Platform _platform;
        
        
        private static Version _modelMapperModuleVersion;

        static ModelMapperService(){
            Init();
        }

        public static List<string> ReservedPropertyNames{ get; }=new List<string>();
        public static List<Type> ReservedPropertyTypes{ get; }=new List<Type>();
        public static List<Type> ReservedAttributeTypes{ get; }=new List<Type>();
        public static Dictionary<Type,(Type type,Func<Attribute,object> result)> AttributesMap{ get; }=new Dictionary<Type, (Type, Func<Attribute, object>)>();
        static ISubject<(Type type,IModelMapperConfiguration configuration)> _typesToMap;
        private static string _outputAssembly;

        private static void Init(){
            _typesToMap = Subject.Synchronize(new ReplaySubject<(Type type,IModelMapperConfiguration configuration)>());
            MappedTypes = Observable.Defer(() => {
                return _typesToMap
                    .Distinct(_ => _.type)
                    .Select(_ => {
                        var modelName = _.type.ModelName();
                        var mappedType = _.type.TypeFromPath( modelName);
                        (string code, IEnumerable<Assembly> references)? codeData = null;
                        var types = new[]{mappedType};
                        if (mappedType == null){
                            codeData = _.type.GenerateCode(_.configuration);
                            types = new[]{_.type};
                        }
                        return (types, codeData);
                    })
                    .Aggregate((acc, cu) => {
                        if (acc.codeData != null && cu.codeData != null){
                            var references = acc.codeData.Value.references.Concat(cu.codeData.Value.references).ToArray();
                            var types = acc.types.Concat(cu.types).ToArray();
                            var code = $"{acc.codeData.Value.code}{Environment.NewLine}{cu.codeData.Value.code}";
                            return (types, (code, references));
                        }
                        return (cu.types,null);
                    })
                    .SelectMany(_ => {
                        if (_.codeData.HasValue){
                            var assembly = _.codeData.Value.references.Compile(_.codeData.Value.code);
                            return _.types.Select(type => assembly.GetType(type.ModelName()));
                        }

                        return _.types;
                    })
                    .Publish()
                    .AutoConnect();
            });
            _modelMapperModuleVersion = typeof(ModelMapperService).Assembly.GetName().Version;
            _platform = XafApplicationExtensions.ApplicationPlatform;
            ReservedPropertyNames.Clear();
            ReservedPropertyNames.AddRange(typeof(IModelNode).Properties().Select(info => info.Name));
            ReservedPropertyTypes.AddRange(new[]{ typeof(Type)});
            ReservedAttributeTypes.Add(typeof(DefaultValueAttribute));
            AttributesMap.Clear();
            ExtendModelService.Init();
            _outputAssembly = $@"{Path.GetDirectoryName(typeof(ModelMapperService).Assembly.Location)}\{ModelMapperAssemblyName}{MapperAssemblyName}{_platform}.dll";
        }

        public static IObservable<Type> MappedTypes{ get;private set; }

        public static Type MapToModel<TModelMapperConfiguration>(this (Type type,TModelMapperConfiguration configuration) type) where TModelMapperConfiguration:IModelMapperConfiguration{
            new[]{type}.MapToModel();
            return type.type;
        }

        public static Type[] MapToModel(this IEnumerable<Type> types){
            return types.Select(_ => (_, new ModelMapperConfiguration())).ToArray().MapToModel();
        }

        public static Type MapToModel(this Type type,IModelMapperConfiguration configuration=null){
            return new []{(type,configuration)}.MapToModel().First();
        }

        public static Type[] MapToModel<TModelMapperConfiguration>(this (Type type,TModelMapperConfiguration configuration)[] types) where TModelMapperConfiguration:IModelMapperConfiguration{
            types.Select(_ => (_.type,(IModelMapperConfiguration)_.configuration)).ToObservable().Subscribe(_typesToMap);
            return types.Select(_ => _.type).ToArray();
        }

        public static Type[] MapToModel<TModelMapperConfiguration>(this XafApplication application, params (Type type,TModelMapperConfiguration configuration)[] types) where TModelMapperConfiguration:IModelMapperConfiguration{
            return types.MapToModel();
        }

        public static Type[] MapToModel(this ModuleBase moduleBase, params (Type type,IModelMapperConfiguration configuration)[] types){
            return types.MapToModel();
        }

        static (string code,IEnumerable<Assembly> references)? GenerateCode(this Type type,IModelMapperConfiguration configuration=null){
            var modelName = type.ModelName();
            var propertyInfos = PropertyInfos(type);
            var allAssemblies = AllAssemblies(type, propertyInfos);
            return (Code(type, configuration, modelName, propertyInfos.AdditionalTypes(type)),allAssemblies);
        }

//        static Type Compile(this Type type,IModelMapperConfiguration configuration=null){
//            _outputAssembly = $@"{AppDomain.CurrentDomain.ApplicationPath()}\{ModelMapperAssemblyName}{MapperAssemblyName}{_platform}.dll";
//            var modelName = type.ModelName();
//            var mappedType = TypeFromPath(type, modelName);
//            if (mappedType != null) return mappedType;
//
//            var propertyInfos = PropertyInfos(type);
//            var code = Code(type, configuration, modelName, propertyInfos.AdditionalTypes(type));
//            var allAssemblies = AllAssemblies(type, propertyInfos);
//            return allAssemblies.Compile(code).GetType(modelName);
//        }

//        static Type MapToModel(this Type type,IModelMapperConfiguration configuration=null){
//            _outputAssembly = $@"{AppDomain.CurrentDomain.ApplicationPath()}\{ModelMapperAssemblyName}{MapperAssemblyName}{_platform}.dll";
//            var modelName = type.ModelName();
//            var mappedType = TypeFromPath(type, modelName);
//            if (mappedType != null) return mappedType;
//
//            var propertyInfos = PropertyInfos(type);
//            var code = Code(type, configuration, modelName, propertyInfos.AdditionalTypes(type));
//            var allAssemblies = AllAssemblies(type, propertyInfos);
//            return allAssemblies.Compile(code).GetType(modelName);
//        }

        private static Type TypeFromPath(this Type type, string modelName){
            if (File.Exists(_outputAssembly)){
                var typeVersion = type.Assembly.GetName().Version;
                using (var assemblyDefinition = AssemblyDefinition.ReadAssembly(_outputAssembly)){
                    var isMapped = assemblyDefinition.CustomAttributes.Any(attribute => {
                        if (attribute.AttributeType.ToType() != typeof(ModelMapperServiceAttribute)) return false;
                        return Version.Parse((string) attribute.ConstructorArguments.Last().Value) == typeVersion &&
                               (string) attribute.ConstructorArguments.Skip(1).Take(1).First().Value == type.Assembly.GetName().Name &&
                               (string) attribute.ConstructorArguments.First().Value==type.FullName;

                    });

                    if (isMapped){
                        var versionAttribute = assemblyDefinition.CustomAttributes.First(attribute => attribute.AttributeType.ToType()==typeof(AssemblyFileVersionAttribute));
                        if (Version.Parse(versionAttribute.ConstructorArguments.First().Value.ToString()) ==_modelMapperModuleVersion){
                            return Assembly.LoadFile(_outputAssembly).GetType(modelName);
                        }
                    }
                }
            }

            return null;
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

        private static string Code(Type type, IModelMapperConfiguration configuration,string modelName, Type[] additionalTypes){
            var assemblyDefinitions = type.AssemblyDefinitions( additionalTypes);
            var modelMappersTypeName = $"IModel{modelName}ModelMappers";
            var code = String.Join(Environment.NewLine, new[]{type.AssemblyAttributesCode()}
                .Concat(new[]{TypeCode(type, modelMappersTypeName, assemblyDefinitions,configuration?.ImageName)})
                .Concat(new[]{type.ContainerCode( configuration, modelName, assemblyDefinitions),ModelMappersInterfaceCode( modelMappersTypeName)})
                .Concat(AdditionalTypesCode(type, additionalTypes, assemblyDefinitions)));
            foreach (var assemblyDefinition in assemblyDefinitions){
                assemblyDefinition.Dispose();
            }
            return code;
        }

        private static IEnumerable<string> AdditionalTypesCode(Type type, Type[] additionalTypes,
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


        private static string TypeCode(Type type, string modelMappersTypeName, AssemblyDefinition[] assemblyDefinitions,string imageName){
            var domainLogic = $@"[{typeof(DomainLogicAttribute).FullName}(typeof({modelMappersTypeName}))]public class {modelMappersTypeName}DomainLogic{{public static int? Get_Index({modelMappersTypeName} mapper){{return 0;}}}}{Environment.NewLine}";
            string modelMappersPropertyCode = $"new int? Index{{get;set;}}{Environment.NewLine}{modelMappersTypeName} {ModelMappersNodeName} {{get;}}";
            var typeCode = type.ModelCode(assemblyDefinitions,imageName, additionalPropertiesCode: modelMappersPropertyCode,
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
        
        private static string AssemblyAttributesCode(this Type type){
            var modelMapperServiceAttributeCode = $@"[assembly:{typeof(ModelMapperServiceAttribute).FullName}(""{type.FullName}"",""{type.Assembly.GetName().Name}"",""{type.Assembly.GetName().Version}"")]";
            var assemblyVersionCode = $@"[assembly:{typeof(AssemblyVersionAttribute).FullName}(""{_modelMapperModuleVersion}"")]{Environment.NewLine}[assembly:{typeof(AssemblyFileVersionAttribute).FullName}(""{_modelMapperModuleVersion}"")]";
            return $"{modelMapperServiceAttributeCode}{Environment.NewLine}{assemblyVersionCode}";
        }

        private static string ContainerCode(this Type type, IModelMapperConfiguration configuration, string modelName,AssemblyDefinition[] assemblyDefinitions){
            var defaultContainerName = $"{modelName}{ContainerSuffix}".Substring(6);
            string persistentNameCode = null;
            if (configuration?.CustomContainerPersistentName != null){
                persistentNameCode=$@"[{typeof(ModelPersistentNameAttribute).FullName}(""{configuration.CustomContainerPersistentName}"")]{Environment.NewLine}";
            }

            var browseableCode = configuration.BrowseableCode();
            return type.ModelCode(assemblyDefinitions, configuration?.ImageName, defaultContainerName,
                $"{persistentNameCode}{browseableCode}{modelName} {configuration?.CustomContainerName ?? modelName.Substring(6)}{{get;}}");
        }

        private static string BrowseableCode(this IModelMapperConfiguration configuration){
            var visibilityCriteria = configuration?.VisibilityCriteria;
            visibilityCriteria = visibilityCriteria == null ? "null" : $@"""{visibilityCriteria}""";
            var browseableCode =
                $"[{typeof(ModelMapperBrowsableAttribute).FullName}(typeof({typeof(ModelMapperVisibilityCalculator).FullName}),{visibilityCriteria})]{Environment.NewLine}";
            return browseableCode;
        }

        private static string ModelCode(this Type type, AssemblyDefinition[] assemblyDefinitions,string imageName=null, string customName = null,string propertiesCode = null,string additionalPropertiesCode=null,Type baseType=null,HashSet<Type> mappedTypes=null){
            mappedTypes = mappedTypes ?? new HashSet<Type>();
            baseType = baseType ?? typeof(IModelNodeEnabled);
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

}