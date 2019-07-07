using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Xpand.Source.Extensions.System.String;

namespace Xpand.XAF.Modules.ModelMapper.Services.TypeMapping{
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
    [AttributeUsage(AttributeTargets.Interface|AttributeTargets.Class)]
    public class ModelMapLinkAttribute:Attribute{
        public ModelMapLinkAttribute(string linkedTypeName){
            LinkedTypeName = linkedTypeName;
        }

        public string LinkedTypeName{ get; }
    }

    public static partial class TypeMappingService{
        private static Subject<(Type declaringType,List<ModelMapperPropertyInfo> propertyInfos)> _customizePropertySelection;
        
        static ((string key, string code)[] code, IEnumerable<string> references) ModelCode(this Type type,IModelMapperConfiguration configuration=null){
            var propertyInfos = type.PropertyInfos();
            var infos = propertyInfos.Where(info => info.Name=="Polygons").ToArray();
            var additionalTypes = propertyInfos.AdditionalTypes(type);
            var firstOrDefault = additionalTypes.FirstOrDefault(type1 => type1.Name.Contains("PlanePolygon"));
            var additionalTypesCode = type.AdditionalTypesCode( additionalTypes);
            var @join = string.Join(Environment.NewLine, additionalTypesCode.Select(tuple => tuple.code));
            var containerName = type.ModelMapContainerName( configuration);
            var mapName = type.ModelMapName( configuration:configuration);
            var containerCode = type.ContainerCode( configuration, $"IModel{containerName}", mapName);
            var modelMappersTypeName = $"IModel{containerName}{ModelMappersNodeName}";
            var modelMappersInterfaceCode = ModelMappersInterfaceCode( modelMappersTypeName);
            var typeCode = type.TypeCode(mapName, modelMappersTypeName, configuration?.ImageName);
            var code = new []{typeCode,containerCode,modelMappersInterfaceCode}.Concat(additionalTypesCode).ToArray();
            var allAssemblies = type.AllAssemblies(propertyInfos);
            return (code,allAssemblies);
        }

        private static string AssemblyAttributesCode(this Type type,IModelMapperConfiguration configuration){
            var modelMapperServiceAttributeCode = ModelMapperServiceAttributeCode(type);
            int hashCode = 0;
            if (configuration != null) hashCode = configuration.GetHashCode();
            var modelMapperConfigurationCode = $@"[assembly:{typeof(ModelMapperModelConfigurationAttribute).FullName}(""{type.FullName}"",{hashCode})]{Environment.NewLine}";
            return string.Join(Environment.NewLine, modelMapperConfigurationCode, modelMapperServiceAttributeCode);
        }

        private static string AssemblyVersionCode(){
            var assemblyVersionCode =
                $@"[assembly:{typeof(AssemblyVersionAttribute).FullName}(""{_modelMapperModuleVersion}"")]{Environment.NewLine}[assembly:{typeof(AssemblyFileVersionAttribute).FullName}(""{_modelMapperModuleVersion}"")]";
            return assemblyVersionCode;
        }

        private static string ModelCode(this (ModelMapperPropertyInfo propertyInfo,Type rootType) data){
            if (data.propertyInfo.Name == "Mapper"){
                Debug.WriteLine("");
            }
            string propertyCode = null;
            var propertyInfo = data.propertyInfo;
            if (propertyInfo.PropertyType.IsValueType || propertyInfo.PropertyType == typeof(string)){
                if (propertyInfo.CanRead && propertyInfo.CanWrite){
                    string nullSign = null;
                    var infoPropertyType = propertyInfo.PropertyType.ToString();
                    var isNullAble = propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
                    if (propertyInfo.PropertyType.IsValueType){
                        nullSign = "?";
                    }
                    if (isNullAble){
                        infoPropertyType = propertyInfo.PropertyType.GenericTypeArguments.First().ToString();
                    }
                    propertyCode = $"{infoPropertyType.Replace("+", ".")}{nullSign} {propertyInfo.Name}{{get;set;}}";
                }
            }
            else if (propertyInfo.PropertyType.GetRealType() != propertyInfo.PropertyType){
                propertyCode = $"{(propertyInfo.PropertyType.GetRealType(),data.rootType).ModelName()}s {propertyInfo.Name}{{get;}}";
            }
            else{
                propertyCode = $"{(propertyInfo.PropertyType,data.rootType).ModelName()} {propertyInfo.Name}{{get;}}";
            }
            if (propertyCode != null){
                var attributesCode = $"{propertyInfo.GetCustomAttributesData().ModelCode()}\r\n";
//                var attributesCode = "";
                return $"{attributesCode}{propertyCode}";
            }
            return null;
        }

        private static (string key,string code)[] AdditionalTypesCode(this Type type, Type[] additionalTypes){
            var mappedTypes = new HashSet<Type>(new[]{type});
            var additionalTypesCode = additionalTypes.Where(_ => !mappedTypes.Contains(_))
                .SelectMany(_ => {
                    var modelCode = (_,type).ModelCode(mappedTypes: mappedTypes);
                    mappedTypes.Add(_);
                    var realType = _.GetRealType();
                    (string, string) modelListCode = (null, null);
                    if (realType != _){
                        if (realType.Name.Contains("Legend")){
                            Debug.WriteLine("");
                        }
                        mappedTypes.Add(realType);
                        modelListCode = ($"{modelCode.key}s",$"{Environment.NewLine}public interface {(realType,type).ModelName()}s:{typeof(IModelNode).FullName},{typeof(IModelList).FullName}<{modelCode.key}>{{}}");
                    }
                    return new[]{modelCode,modelListCode};
                }).Where(_ => _!=default);
            return additionalTypesCode.ToArray();
        }

        private static (string key, string code) TypeCode(this Type type, string mapName, string modelMappersTypeName, string imageName){

            var domainLogic = $@"[{typeof(DomainLogicAttribute).FullName}(typeof({modelMappersTypeName}))]{Environment.NewLine}public class {modelMappersTypeName}DomainLogic{{public static int? Get_Index({modelMappersTypeName} mapper){{return 0;}}}}{Environment.NewLine}";
            string modelMappersPropertyCode = $"new int? Index{{get;set;}}{Environment.NewLine}{modelMappersTypeName} {ModelMappersNodeName} {{get;}}";
            var typeCode = (type,type).ModelCode(imageName,mapName, additionalPropertiesCode: modelMappersPropertyCode,
                baseType: typeof(IModelModelMap));
            return (typeCode.key, $"{domainLogic}{typeCode.code}");
        }

        private static (string key, string code) ModelMappersInterfaceCode(string modelMappersTypeName){

            string modelMapperContextContainerName=typeof(IModelMapperContextContainer).FullName;
            var nodesGeneratorName=typeof(ModelMapperContextNodeGenerator).FullName;
            var imageCode=$@"[{typeof(ImageNameAttribute).FullName}(""{ModelImageSource.ModelModelMapperContexts}"")]{Environment.NewLine}";
            string modelGeneratorCode=$"[{typeof(ModelNodesGeneratorAttribute)}(typeof({nodesGeneratorName}))]{Environment.NewLine}";
            var descriptionCode=$@"[{typeof(DescriptionAttribute)}(""These mappers relate to Application.ModelMapper.MapperContexts and applied first."")]{Environment.NewLine}";
            var modelMappersInterfaceCode =
                $@"{descriptionCode}{modelGeneratorCode}{imageCode}public interface {modelMappersTypeName}:{typeof(IModelList).FullName}<{modelMapperContextContainerName}>,{typeof(IModelNode).FullName}{{}}";
            return (modelMappersTypeName,modelMappersInterfaceCode);
        }

        private static string ModelMapperServiceAttributeCode(this Type type){
            return $@"[assembly:{typeof(ModelMapperServiceAttribute).FullName}(""{type.FullName}"",""{type.Assembly.GetName().Name}"",""{type.Assembly.GetName().Version}"")]";
        }

        private static (string key, string code) ContainerCode(this Type type, IModelMapperConfiguration configuration, string modelName, string mapName){

            var modelBrowseableCode = configuration.ModelBrowseableCode();
            var linkAttributeCode = $@"[{typeof(ModelMapLinkAttribute).FullName}(""{type.AssemblyQualifiedName}"")]{Environment.NewLine}";
            var propertiesCode = $"{modelBrowseableCode}IModel{mapName} {mapName}{{get;}}";
            
            var containerCode = (type,Type.EmptyTypes.FirstOrDefault()).ModelCode(null, $"{modelName}".Substring(6),
                propertiesCode,baseType:typeof(IModelModelMapContainer));
            
            return (containerCode.key,$"{linkAttributeCode}{containerCode.code}");
        }

        private static string ModelBrowseableCode(this IModelMapperConfiguration configuration){
            var visibilityCriteria = configuration?.VisibilityCriteria;
            visibilityCriteria = visibilityCriteria == null ? "null" : $@"""{visibilityCriteria}""";
            return $"[{typeof(ModelMapperBrowsableAttribute).FullName}(typeof({typeof(ModelMapperVisibilityCalculator).FullName}),{visibilityCriteria})]{Environment.NewLine}";
        }

        private static (string key, string code) ModelCode(this (Type typeToCode, Type rootType) data, string imageName = null,
            string customName = null, string propertiesCode = null,string additionalPropertiesCode = null, Type baseType = null, HashSet<Type> mappedTypes = null){

            mappedTypes = mappedTypes ?? new HashSet<Type>();
            baseType = baseType ?? typeof(IModelNodeDisabled);
            var typeToCode =data.typeToCode==data.rootType?data.typeToCode: data.typeToCode.GetRealType();
            if (typeToCode == typeof(object)){
                typeToCode = data.typeToCode;
            }

            if (propertiesCode == null){
                var propertyInfos = typeToCode.PublicProperties(true)
                    .Where(info => !mappedTypes.Contains(info.PropertyType))
                    .ToModelMapperPropertyInfo();
                var properties = new List<ModelMapperPropertyInfo>(propertyInfos);
                _customizePropertySelection.OnNext((typeToCode,properties));
                propertiesCode =  String.Join(Environment.NewLine,properties.Select(propertyInfo =>(propertyInfo,data.rootType).ModelCode()));
            }

            propertiesCode += $"{Environment.NewLine}{additionalPropertiesCode}";
            string imageCode = null;
            if (imageName!=null){
                imageCode = $@"[{typeof(ImageNameAttribute).FullName}({imageName.ToLiteral()})]{Environment.NewLine}";
            }

            var modelName = $"{(typeToCode,data.rootType).ModelName(customName)}";
            return (modelName,$"{imageCode}public interface {modelName}:{baseType.FullName}{{{Environment.NewLine}{propertiesCode}{Environment.NewLine}}}");
        }

        private static string ModelName(this (Type typeToCode,Type rootType) data,string customName=null){
            if (customName!=null){
                return $"IModel{customName}";
            }

            return $"IModel{data.typeToCode.Namespace.CleanCodeName()}{data.rootType.Name}{data.typeToCode.Name}";
        }

        private static IObservable<(string code, IEnumerable<string> references)> ModelCode(this IObservable<(Type type, IModelMapperConfiguration configuration)> source){

            var assemblyAttributes = source.Select(_ => _.type.AssemblyAttributesCode(_.configuration))
                .Concat(Observable.Return(AssemblyVersionCode()))
                .Select(_=>(code:_,references:new[]{typeof(ModelMapperAttribute).Assembly.Location}.AsEnumerable()));
            var typeCode = source.SelectMany(_ => {
                    var code = _.type.ModelCode(_.configuration);
                    return code.code.Select(__ => (code: __, code.references));
                })
                .Distinct(_ => _.code.key)
                .Select(_ => (_.code.code, _.references));
            return assemblyAttributes.Concat(typeCode)
                .Aggregate((acc, cu) => {
                    var code = string.Join(Environment.NewLine, acc.code, cu.code);
                    return (code, acc.references.Concat(cu.references).Distinct());
                });
        }
    }
}