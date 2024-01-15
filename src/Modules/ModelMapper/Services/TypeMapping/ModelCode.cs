using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.ReflectionExtensions;
using Xpand.Extensions.StringExtensions;
using Xpand.XAF.Modules.ModelMapper.Configuration;

namespace Xpand.XAF.Modules.ModelMapper.Services.TypeMapping{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class ModelMapperServiceAttribute(string hashCode) : Attribute {
        public string HashCode{ get; } = hashCode;
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class ModelMapperTypeAttribute(
        string mappedType,
        string mappedAssembly,
        string assemblyHashCode,
        string configurationHashCode)
        : Attribute {
        public string MappedAssembly{ get; } = mappedAssembly;

        public string AssemblyHashCode{ get; } = assemblyHashCode;
        public string MappedType{ get; } = mappedType;
        public string ConfigurationHashCode{ get; } = configurationHashCode;
    }

    [AttributeUsage(AttributeTargets.Interface|AttributeTargets.Class)]
    public class ModelMapLinkAttribute(string linkedTypeName) : Attribute {
        public string LinkedTypeName{ get; } = linkedTypeName;
    }

    public class Result<T>{

        public T Data{ get; set; }
    }
    public static partial class TypeMappingService{
        private static Subject<(Type type, Result<(string key, string code)> result)> _customizeContainerCode;
        private static Subject<(Type declaringType,List<ModelMapperPropertyInfo> propertyInfos)> _customizeProperties;
        private static Subject<GenericEventArgs<ModelMapperType>> _customizeTypes;
        
        static ((string key, string code,bool map)[] code, IEnumerable<string> references) ModelCode(this Type type,IModelMapperConfiguration configuration=null){
            var propertyInfos = type.PropertyInfos().Concat(AdditionalTypesList.Select(t => t.GetRealType()).SelectMany(t => t.PropertyInfos())).ToArray();
            var additionalTypes = propertyInfos.AdditionalTypes(type)
                .Concat(AdditionalTypesList).Distinct().ToArray();
            var additionalTypesCode = type.AdditionalTypesCode( additionalTypes);
            var containerName = type.ModelMapContainerName( configuration);
            var mapName = type.ModelTypeName(type, configuration);
            var containerCode = type.ContainerCode( configuration, $"IModel{containerName}", mapName);
            var modelMappersTypeName = $"IModel{containerName}{ModelMappersNodeName}";
            var modelMappersInterfaceCode = ModelMappersInterfaceCode( modelMappersTypeName);
            var typeCode = type.TypeCode(mapName, modelMappersTypeName, configuration);
            
            var code = new []{typeCode,containerCode,modelMappersInterfaceCode}.Concat(additionalTypesCode).Where(t => t!=default).ToArray();
            var infos = propertyInfos.Concat(type.PublicProperties()).Distinct().ToArray();
            var references = type.References(infos,additionalTypes);
            return (code,references);
        }

        private static string AssemblyAttributesCode(this Type type,IModelMapperConfiguration configuration){
            var modelMapperServiceAttributeCode = type.ModelMapperServiceAttributeCode(configuration);
            var modelMapperConfigurationCode = $@"[assembly:{typeof(ModelMapperServiceAttribute).FullName}(""{HashCode()}"")]{Environment.NewLine}";
            return string.Join(Environment.NewLine, modelMapperConfigurationCode, modelMapperServiceAttributeCode);
        }

        private static string AssemblyVersionCode()
            => $@"[assembly:{typeof(AssemblyVersionAttribute).FullName}(""{_modelMapperModuleVersion}"")]{Environment.NewLine}[assembly:{typeof(AssemblyFileVersionAttribute).FullName}(""{_modelMapperModuleVersion}"")]";

        private static string ModelCode(this (ModelMapperPropertyInfo propertyInfo,Type rootType) data){
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
                return $"{attributesCode}{propertyCode}";
            }
            return null;
        }

        private static (string key,string code,bool map)[] AdditionalTypesCode(this Type type, Type[] additionalTypes){
            var mappedTypes = new HashSet<Type>(new[]{type});
            var additionalTypesCode = additionalTypes.Where(t => !mappedTypes.Contains(t))
                .SelectMany(t => {
                    var modelCode = (t,type).ModelCode();
                    var realType = t.GetRealType();
                    (string, string,bool) modelListCode = (null, null,false);
                    if (realType != t){
                        mappedTypes.AddMappedType(realType);
                        if (!realType.IsGenericType||realType.IsNullableType()) {
                            modelListCode = ($"{modelCode.key}s",$"{Environment.NewLine}public interface {(realType,type).ModelName()}s:{typeof(IModelNode).FullName},{typeof(IModelList).FullName}<{modelCode.key}>{{}}",false);
                        }
                    }
                    return new[]{modelCode,modelListCode};
                }).Where(t => t!=default);
            return additionalTypesCode.OrderBy(t => t.Item1).ToArray();
        }

        private static void AddMappedType(this HashSet<Type> mappedTypes,Type type){
            if (!type.IsValueType && type != typeof(string)){
                mappedTypes.Add(type);
            }
        }

        private static (string key, string code, bool map) TypeCode(this Type type, string mapName,
            string modelMappersTypeName, IModelMapperConfiguration configuration){

            var domainLogic = $@"[{typeof(DomainLogicAttribute).FullName}(typeof({modelMappersTypeName}))]{Environment.NewLine}public class {modelMappersTypeName}DomainLogic{{public static int? Get_Index({modelMappersTypeName} mapper){{return 0;}}}}{Environment.NewLine}";
            var modelMappersPropertyCode = ModelMappersPropertyCode(modelMappersTypeName);
            var displayText = (configuration?.DisplayName ?? type.Name).ToLiteral();
            var displayNameAttribute=$@"[{typeof(ModelDisplayNameAttribute)}({displayText})]{Environment.NewLine}";
            var typeCode = (type,type).ModelCode(configuration?.ImageName,mapName, additionalPropertiesCode: modelMappersPropertyCode,baseType: typeof(IModelModelMap));
            return (typeCode.key, $"{domainLogic}{displayNameAttribute}{typeCode.code}",true);
        }

        private static string ModelMappersPropertyCode(string modelMappersTypeName){
            string modelMappersPropertyCode =$"new int? Index{{get;set;}}{Environment.NewLine}{modelMappersTypeName} {ModelMappersNodeName} {{get;}}";
            return modelMappersPropertyCode;
        }

        private static (string key, string code,bool map) ModelMappersInterfaceCode(string modelMappersTypeName){

            string modelMapperContextContainerName=typeof(IModelMapperContextContainer).FullName;
            var nodesGeneratorName=typeof(ModelMapperContextNodeGenerator).FullName;
            var imageCode=$@"[{typeof(ImageNameAttribute).FullName}(""{ModelImageSource.ModelModelMapperContexts}"")]{Environment.NewLine}";
            string modelGeneratorCode=$"[{typeof(ModelNodesGeneratorAttribute)}(typeof({nodesGeneratorName}))]{Environment.NewLine}";
            var descriptionCode=$@"[{typeof(DescriptionAttribute)}(""These mappers relate to Application.ModelMapper.MapperContexts and applied first."")]{Environment.NewLine}";
            var modelMappersInterfaceCode =
                $@"{descriptionCode}{modelGeneratorCode}{imageCode}public interface {modelMappersTypeName}:{typeof(IModelList).FullName}<{modelMapperContextContainerName}>,{typeof(IModelNode).FullName}{{}}";
            return (modelMappersTypeName,modelMappersInterfaceCode,false);
        }

        private static string ModelMapperServiceAttributeCode(this Type type, IModelMapperConfiguration configuration)
            => $@"[assembly:{typeof(ModelMapperTypeAttribute).FullName}" +
               $@"(""{type.FullName}"",""{type.Assembly.GetName().Name}""," +
               $@"""{type.Assembly.ManifestModule.ModuleVersionId}"",""{configuration.ToString().ToGuid()}"")]";

        private static (string key, string code,bool map) ContainerCode(this Type type, IModelMapperConfiguration configuration, string modelName, string mapName){
            if (!configuration.OmitContainer){
                var modelBrowsableCode = configuration.ModelBrowsableCode();
                var linkAttributeCode = $@"[{typeof(ModelMapLinkAttribute).FullName}(""{type.AssemblyQualifiedName}"")]{Environment.NewLine}";
                string propertiesCode = null;
                var result = new Result< (string key, string code)>();
                _customizeContainerCode.OnNext((type, result));
                var instanceData = result.Data;
                string key = null;
                if (instanceData != default){
                    key = instanceData.key;
                    propertiesCode = instanceData.code;
                }
                propertiesCode ??= $"{modelBrowsableCode}{mapName} {configuration.MapName??type.Name}{{get;}}";
                var containerCode = (type,Type.EmptyTypes.FirstOrDefault()).ModelCode(null, $"{modelName}".Substring(6),
                    propertiesCode,baseType:typeof(IModelModelMapContainer));
            
                key ??= containerCode.key;
                return (key,$"{linkAttributeCode}{containerCode.code}",false);
            }

            return default;
        }

        private static string ModelBrowsableCode(this IModelMapperConfiguration configuration){
            var visibilityCriteria = configuration?.VisibilityCriteria;
            visibilityCriteria = visibilityCriteria == null ? "null" : $@"""{visibilityCriteria}""";
            return $"[{typeof(ModelMapperBrowsableAttribute).FullName}(typeof({typeof(ModelMapperVisibilityCalculator).FullName}),{visibilityCriteria})]{Environment.NewLine}";
        }

        private static (string key, string code,bool map) ModelCode(this (Type typeToCode, Type rootType) data, string imageName = null,
            string customName = null, string propertiesCode = null,string additionalPropertiesCode = null, Type baseType = null, HashSet<Type> mappedTypes = null){

            baseType ??= typeof(IModelNodeDisabled);
            var typeToCode =data.typeToCode==data.rootType?data.typeToCode: data.typeToCode.GetRealType();
            if (typeToCode == typeof(object)){
                typeToCode = data.typeToCode;
            }
            if (propertiesCode == null){
                var propertyInfos = typeToCode.PublicProperties()
                    .Where(info => mappedTypes == null || !mappedTypes.Contains(info.PropertyType))
                    .Select(info => {
                        mappedTypes?.AddMappedType(info.PropertyType);
                        return info;
                    })
                    .Where(info => info.PropertyType!=data.rootType)
                    .ToModelMapperPropertyInfo().ToArray();
                var properties = new List<ModelMapperPropertyInfo>(propertyInfos);
                _customizeProperties.OnNext((typeToCode,properties));
                propertiesCode =  String.Join(Environment.NewLine,properties.DistinctWith(info => info.Name).Select(propertyInfo =>(propertyInfo,data.rootType).ModelCode()));
            }
            
            string imageCode = null;
            if (imageName!=null){
                imageCode = $@"[{typeof(ImageNameAttribute).FullName}({imageName.ToLiteral()})]{Environment.NewLine}";
            }

            var modelName = $"{(typeToCode,data.rootType).ModelName(customName)}";
            var args = new GenericEventArgs<ModelMapperType>(new ModelMapperType(typeToCode,data.rootType,modelName,additionalPropertiesCode));

            var modelMapperType = args.Instance;
            modelMapperType.BaseTypeFullNames.Add(baseType.FullName);
            _customizeTypes.OnNext(args);
            if (!args.Handled) {
                propertiesCode += $"{Environment.NewLine}{modelMapperType.AdditionalPropertiesCode}";
                var baseTypes = string.Join(",",modelMapperType.BaseTypeFullNames.Distinct());
                if (!string.IsNullOrEmpty(baseTypes)){
                    baseTypes = $":{baseTypes}";
                }
            
                var attributesCode = $"{modelMapperType.CustomAttributeData.ModelCode()}{Environment.NewLine}";
                return (modelName,$"{imageCode}{Environment.NewLine}{attributesCode}public interface {modelMapperType.ModelName}{baseTypes}{{{Environment.NewLine}{propertiesCode}{Environment.NewLine}}}",false);
            }

            return default;
        }

        private static string ModelName(this (Type typeToCode,Type rootType) data,string customName=null) 
            => customName != null
                ? !customName.StartsWith("IModel") ? $"IModel{customName}" : customName
                : $"IModel{data.typeToCode.Namespace?.Replace(".", "")}_{data.typeToCode.Name}";

        private static IObservable<(string code, IEnumerable<string> references)> ModelCode(this IObservable<IModelMapperConfiguration> source) 
            => source.AssemblyCode().Concat(source.SelectMany(configuration => Observable.Start(() => {
                        var code = configuration.TypeToMap.ModelCode(configuration);
                        return code.code.Select(t => (code: t, code.references)).ToArray();
                    },ModelCodeScheduler))
                    .SelectMany()
                    .RemoveDuplicates())
                .Aggregate((acc, cu) => {
                    var code = string.Join(Environment.NewLine, acc.code, cu.code);
                    return (code, acc.references.Concat(cu.references).Distinct());
                }).TraceModelMapper();

        private static IObservable<(string code, IEnumerable<string> references)> AssemblyCode(this IObservable<IModelMapperConfiguration> source) 
            => source.Select(configuration => configuration.TypeToMap.AssemblyAttributesCode(configuration))
                .Aggregate((acc, curr) => {
                    acc += curr;
                    return acc;
                })
                .Concat(Observable.Return(AssemblyVersionCode()))
                .Select(s=>(code:s,references:new[]{typeof(ModelMapperModule).Assembly.Location}.AsEnumerable()));

        private static IObservable<(string code, IEnumerable<string> references)> RemoveDuplicates(this 
            IObservable<((string key, string code, bool map) code, IEnumerable<string> references)> modelCode) 
            => modelCode.ToEnumerable()
                .GroupBy(t => t.code.key).SelectMany(t => t.OrderByDescending(tuple => tuple.code.map).Take(1))
                .ToObservable(ImmediateScheduler.Instance)
                .Select(t => (t.code.code, t.references));

        public static IScheduler ModelCodeScheduler{ get; set; }=Scheduler.Default;
    }
}