using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.Persistent.Base;
using Fasterflect;

namespace Xpand.XAF.Modules.ModelMapper.Services.TypeMapping{
    public static partial class TypeMappingService{
        public static string DefaultContainerSuffix="Map";
        public static string ModelMapperAssemblyName=null;
        public static string MapperAssemblyName="ModelMapperAssembly";
        public static string ModelMappersNodeName="ModelMappers";
        public static List<string> ReservedPropertyNames{ get; }=new List<string>();
        public static List<Type> ReservedPropertyTypes{ get; }=new List<Type>();
        public static List<Type> ReservedPropertyInstances{ get; }=new List<Type>();
        public static List<Type> AdditionalTypesList{ get; }=new List<Type>();
        static ISubject<(Type type,IModelMapperConfiguration configuration)> _typesToMap;
        public static List<(string key, Action<(Type declaringType,List<ModelMapperPropertyInfo> propertyInfos)> action)> PropertyMappingRules{ get; private set; }
        public static List<(string key, Action<ModelMapperType> action)> TypeMappingRules{ get; private set; }

        static TypeMappingService(){
            Init();
        }

        public static void Start(){
            ConnectCustomizationRules();
            _typesToMap.OnCompleted();
        }

        internal static IObservable<Unit> Connect(){
            return Observable.Return(Unit.Default);
        }

        public static string OutPutAssembly =>
            $@"{Path.GetDirectoryName(typeof(TypeMappingService).Assembly.Location)}\{ModelMapperAssemblyName}{MapperAssemblyName}{ModelExtendingService.Platform}.dll";

        private static void Init(){
            _customizeProperties =new Subject<(Type declaringType, List<ModelMapperPropertyInfo> propertyInfos)>();
            _customizeTypes =new Subject<ModelMapperType>();
            AdditionalReferences=new List<Type>(new []{typeof(IModelNode),typeof(DescriptionAttribute),typeof(AssemblyFileVersionAttribute),typeof(ImageNameAttribute),typeof(TypeMappingService)});
            TypeMappingRules = new List<(string key, Action<ModelMapperType> action)>(){
                (nameof(WithNonPublicAttributeParameters), NonPublicAttributeParameters),
                (nameof(GenericTypeArguments), GenericTypeArguments),
            };
            PropertyMappingRules = new List<(string key, Action<(Type declaringType, List<ModelMapperPropertyInfo> propertyInfos)> action)>{
                (nameof(GenericTypeArguments), GenericTypeArguments),
                (nameof(BrowsableRule), BrowsableRule),
                (nameof(PrivateDescriptionRule), PrivateDescriptionRule),
                (nameof(DefaultValueRule), DefaultValueRule),
                (nameof(WithNonPublicAttributeParameters), NonPublicAttributeParameters),
                (nameof(TypeConverterWithDXDesignTimeType), TypeConverterWithDXDesignTimeType)
            };
            _typesToMap = new ReplaySubject<(Type type,IModelMapperConfiguration configuration)>();
            MappedTypes = Observable.Defer(() => {
                var distinnctTypesToMap = _typesToMap.Distinct(_ => _.type);
                return distinnctTypesToMap
                    .All(_ => _.TypeFromPath())
                    .Select(_ =>!_? distinnctTypesToMap.ModelCode().Compile(): Assembly.LoadFile(OutputAssembly).GetTypes()
                                .Where(type => typeof(IModelModelMap).IsAssignableFrom(type)).ToObservable()).Switch();
            }).Publish().AutoConnect().Replay().AutoConnect();
            _modelMapperModuleVersion = typeof(TypeMappingService).Assembly.GetName().Version;
            
            ReservedPropertyNames.Clear();
            var names = new []{typeof(ModelNode),typeof(IModelNode),typeof(ModelApplicationBase)}
                .SelectMany(_ => _.GetMembers()).Select(_ => _.Name)
                .Concat(new []{"Item","IsReadOnly","Remove","Id","Nodes","IsValid"}).Distinct();
            ReservedPropertyNames.AddRange(names);
            ReservedPropertyTypes.AddRange(new[]{ typeof(Type),typeof(IList),typeof(object),typeof(Array),typeof(IComponent),typeof(ISite)});
            ReservedPropertyInstances.AddRange(new[]{ typeof(IDictionary)});
            var systemWebAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetName().Name == "System.Web");
            if (systemWebAssembly != null){
                var type = systemWebAssembly.GetType("System.Web.HttpCookie");
                ReservedPropertyTypes.Add(type);
            }
            ModelExtendingService.Init();
            
        }

        public static IEnumerable<Type> ModelMapperContainerType(this Type type){
            return type.Assembly.GetTypes()
                .Where(_ => _.Name.EndsWith(DefaultContainerSuffix))
                .Where(_ => _.GetInterfaces().Contains(typeof(IModelModelMapContainer)));
        }

        public static IObservable<Type> MappedTypes{ get;private set; }
        public static List<Type> AdditionalReferences{ get; private set; }

        public static IEnumerable<ModelMapperPropertyInfo> ToModelMapperPropertyInfo(this IEnumerable<PropertyInfo> source){
            return source.Select(_ => _.ToModelMapperPropertyInfo());
        }

        public static ModelMapperPropertyInfo ToModelMapperPropertyInfo(this PropertyInfo propertyInfo){
            return new ModelMapperPropertyInfo(propertyInfo);
        }

        public static IEnumerable<ModelMapperCustomAttributeData> ToModelMapperConfigurationData(this IEnumerable<CustomAttributeData> source){
            return source.Select(_ => new ModelMapperCustomAttributeData(_.AttributeType, _.ConstructorArguments));
        }
        public static IObservable<Type> MapToModel<TModelMapperConfiguration>(this (Type type, TModelMapperConfiguration configuration) type)
            where TModelMapperConfiguration : IModelMapperConfiguration{

            return new[]{type}.MapToModel();
        }

        public static IObservable<Type> MapToModel(this IObservable<Type> types){
            return types.Select(_ => (_, new ModelMapperConfiguration())).MapToModel();
        }

        public static IObservable<Type> MapToModel(this IEnumerable<Type> types){
            return types.ToArray().ToObservable().MapToModel();
        }

        public static IObservable<Type> ModelInterfaces(this IObservable<Type> source){
            return source.Finally(Start).Select(_ => MappedTypes).Switch();
        }

        public static IObservable<Type> MapToModel(this Type type,IModelMapperConfiguration configuration=null){
            return new []{(type,configuration)}.MapToModel();
        }

        public static IObservable<Type> MapToModel<TModelMapperConfiguration>(
            this IObservable<(Type type, TModelMapperConfiguration configuration)> types)
            where TModelMapperConfiguration : IModelMapperConfiguration{
            return types
                .Select(_ => (_.type, (IModelMapperConfiguration) _.configuration))
                .Do(_ => _typesToMap.OnNext(_))
                .Select(_ => _.type);
        }

        public static IObservable<Type> MapToModel<TModelMapperConfiguration>(this (Type type,TModelMapperConfiguration configuration)[] types) where TModelMapperConfiguration:IModelMapperConfiguration{
            return types.ToObservable(Scheduler.Immediate).MapToModel();

        }

        public static IObservable<Type> MapToModel<TModelMapperConfiguration>(this XafApplication application, params (Type type,TModelMapperConfiguration configuration)[] types) where TModelMapperConfiguration:IModelMapperConfiguration{
            return types.MapToModel();
        }

        public static IObservable<Type> MapToModel(this ModuleBase moduleBase, params (Type type,IModelMapperConfiguration configuration)[] types){
            return types.MapToModel();
        }

        public static string ModelMapContainerName(this Type type, IModelMapperConfiguration configuration=null){
            return configuration?.ContainerName?? $"{type.Name}{DefaultContainerSuffix}";
        }

        public static string ModelMapName(this Type type,Type rootType=null, IModelMapperConfiguration configuration=null){
            if (rootType==null){
                return configuration?.MapName??type.Name;
            }

            return (type, rootType).ModelName(configuration?.MapName);
        }

    }
}