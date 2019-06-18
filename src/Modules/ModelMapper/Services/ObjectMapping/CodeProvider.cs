using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text.RegularExpressions;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using EnumsNET.NonGeneric;
using Fasterflect;
using Mono.Cecil;
using Xpand.Source.Extensions.MonoCecil;
using Xpand.Source.Extensions.System.String;

namespace Xpand.XAF.Modules.ModelMapper.Services.ObjectMapping{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class ModelMapperModelConfigurationAttribute : Attribute{
        public ModelMapperModelConfigurationAttribute(string typeName, int hashCode){
            TypeName = typeName;
            HashCode = hashCode;
        }

        public string TypeName{ get; }
        public int HashCode{ get; }
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class ModelMapperServiceAttribute : Attribute{
        public ModelMapperServiceAttribute(string mappedType, string mappedAssemmbly, string version){
            MappedAssemmbly = mappedAssemmbly;
            MappedType = mappedType;
            Version = Version.Parse(version);
        }

        public string MappedAssemmbly{ get; }

        public Version Version{ get; }
        public string MappedType{ get; }
    }

    public static partial class ObjectMappingService{
        private static readonly Subject<CustomizeAttribute> CustomizeAttributesSubject=new Subject<CustomizeAttribute>();
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

        private static IEnumerable<(Attribute attribute, CustomAttribute customAttribute)> AttributeData(
            this IEnumerable<(CustomAttribute customAttribute, PropertyInfo propertyInfo)> source){
            var data = source.Select(_ => {
                var results = _.propertyInfo.Attributes<Attribute>()
                    .Where(attribute => _.customAttribute.AttributeType.ToType()==attribute.GetType())
                    .Select(attribute => (attribute:attribute.Find(_.customAttribute), _.customAttribute));
                var firstOrDefault = results.FirstOrDefault(tuple => tuple.attribute!=null);
                return firstOrDefault;
            }).Where(_ => _!=default).ToArray();
            return data;
        }

        private static Attribute Find(this Attribute attribute, CustomAttribute customAttribute){
            var hasFlags = customAttribute.ConstructorArguments.Any(_ =>_.Type.ToType().IsEnum&& NonGenericEnums.GetMember(_.Type.ToType(), _.Value)==null);
            if (!hasFlags){
                var instance = CreateAttributeInstance(attribute, customAttribute.ConstrctorArgumentsValues());
                if (instance.Equals(attribute))
                    return attribute;
            }
            return null;
        }

        private static object[] ConstrctorArgumentsValues(this CustomAttribute customAttribute){
            var args = customAttribute.ConstructorArguments.Select(_ => {
                var value = _.Value;
                if (value is TypeReference typeReference){
                    return typeReference.ToType();
                }

                var type = _.Type.ToType();
                return type.IsEnum ? NonGenericEnums.GetMember(type, value).Value : value;
            }).ToArray();
            return args;
        }

        private static object CreateAttributeInstance(this Attribute attribute, object[] args){
            object instance;
            try{
                instance = Activator.CreateInstance(attribute.GetType(), args);
            }
            catch (Exception){
                instance = attribute.GetType().TryCreateInstanceWithValues(args);
            }
            return instance;
        }

        private static string AttributesCode(this PropertyInfo propertyInfo,TypeDefinition typeDefinition){
            var customAttributes = typeDefinition.Properties
                .Where(_ => _.Name == propertyInfo.Name)
                .SelectMany(_ => _.CustomAttributes.Select(attribute => (attribute,propertyInfo)).AttributeData().Take(1))
                .ToArray();

            
            var customizeAttribute = new CustomizeAttribute()
                {Attributes = new List<(Attribute attribute, CustomAttribute customAttribute)>(customAttributes), PropertyInfo = propertyInfo};
            CustomizeAttributesSubject.OnNext(customizeAttribute);
            var attributesCode = $"{customizeAttribute.Attributes.ModelCode()}\r\n";
            return attributesCode;
        }

        private static string AssemblyAttributesCode(this Type type,IModelMapperConfiguration configuration){
            var modelMapperServiceAttributeCode = ModelMapperServiceAttributeCode(type);
            var assemblyVersionCode = $@"[assembly:{typeof(AssemblyVersionAttribute).FullName}(""{_modelMapperModuleVersion}"")]{Environment.NewLine}[assembly:{typeof(AssemblyFileVersionAttribute).FullName}(""{_modelMapperModuleVersion}"")]";
            int hashCode = 0;
            if (configuration != null) hashCode = configuration.GetHashCode();
            var modelMapperConfigurationCode = $@"[assembly:{typeof(ModelMapperModelConfigurationAttribute).FullName}(""{type.FullName}"",{hashCode})]{Environment.NewLine}";
            return string.Join(Environment.NewLine, modelMapperConfigurationCode, assemblyVersionCode, modelMapperServiceAttributeCode);
        }

        private static string ModelCode(this PropertyInfo propertyInfo,TypeDefinition typeDefinition){
            string propertyCode = null;
            if (propertyInfo.PropertyType.IsValueType || propertyInfo.PropertyType == typeof(string)){
                if (propertyInfo.CanRead && propertyInfo.CanWrite){
                    string nullSign = null;
                    var infoPropertyType = propertyInfo.PropertyType.ToString();
                    var isNullAble = propertyInfo.PropertyType.IsGenericType &&
                                     propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
                    if (propertyInfo.PropertyType.IsValueType){
                        nullSign = "?";
                    }

                    if (isNullAble){
                        infoPropertyType = propertyInfo.PropertyType.GenericTypeArguments.First().ToString();
                    }

                    propertyCode = $"{infoPropertyType.Replace("+", ".")}{nullSign} {propertyInfo.Name}{{get;set;}}";
                }
            }
            else{
                propertyCode = $"{propertyInfo.PropertyType.ModelName()} {propertyInfo.Name}{{get;}}";
            }

            if (propertyCode != null){
                var attributesCode = propertyInfo.AttributesCode(typeDefinition);

                return $"{attributesCode}{propertyCode}";
            }

            return null;
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

        private static string TypeCode(this Type type, string mapName, string modelMappersTypeName,
            AssemblyDefinition[] assemblyDefinitions, string imageName){

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

                private static string ModelMapperServiceAttributeCode(this Type type){
            return $@"[assembly:{typeof(ModelMapperServiceAttribute).FullName}(""{type.FullName}"",""{type.Assembly.GetName().Name}"",""{type.Assembly.GetName().Version}"")]";
        }

        private static string ContainerCode(this Type type, IModelMapperConfiguration configuration, string modelName,
            AssemblyDefinition[] assemblyDefinitions, string mapName){
            var modelBrowseableCode = configuration.ModelBrowseableCode();
            return type.ModelCode(assemblyDefinitions, null, $"{modelName}".Substring(6),
                $"{modelBrowseableCode}IModel{mapName} {mapName}{{get;}}",baseType:typeof(IModelModelMapContainer));
        }

        private static string ModelBrowseableCode(this IModelMapperConfiguration configuration){
            var visibilityCriteria = configuration?.VisibilityCriteria;
            visibilityCriteria = visibilityCriteria == null ? "null" : $@"""{visibilityCriteria}""";
            var browseableCode =
                $"[{typeof(ModelMapperBrowsableAttribute).FullName}(typeof({typeof(ModelMapperVisibilityCalculator).FullName}),{visibilityCriteria})]{Environment.NewLine}";
            return browseableCode;
        }

        private static string ModelCode(this Type type, AssemblyDefinition[] assemblyDefinitions,string imageName = null, string customName = null, string propertiesCode = null,
            string additionalPropertiesCode = null, Type baseType = null, HashSet<Type> mappedTypes = null){

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
            propertiesCode = propertiesCode ?? String.Join(Environment.NewLine,properties.Select(propertyInfo => propertyInfo.ModelCode( typeDefinition)));
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

        private static IObservable<((Type type, IModelMapperConfiguration configuration)[] typeData, (string code,IEnumerable<Assembly> references)? codeData)>
            GenerateCode(this IObservable<(Type type, IModelMapperConfiguration configuration)> source){

            return source.Select(_ =>
                    (types: new[]{(_.type, _.configuration)}, codeData: _.type.GenerateCode(_.configuration)))
                .Aggregate((acc, cu) => {
                    if (acc.codeData != null && cu.codeData != null){
                        var references = acc.codeData.Value.references.Concat(cu.codeData.Value.references).ToArray();
                        var types = acc.types.Concat(cu.types).ToArray();
                        var code = cu.codeData.Value.code;
                        var assemblyAttributes = AssemblyAttributes(code);
                        code = Regex.Replace(code, @"\[assembly:[^\]]*\]", "");
                        var accCode = $"{assemblyAttributes}{Environment.NewLine}{acc.codeData.Value.code}{Environment.NewLine}{code}";
                        return (types, (accCode, references));
                    }

                    return (cu.types, null);
                });
        }
    }
}