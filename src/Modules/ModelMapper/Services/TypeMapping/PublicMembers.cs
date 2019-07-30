using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using ConcurrentCollections;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.Persistent.Base;
using Fasterflect;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services.Predefined;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.ModelMapper.Services.TypeMapping{
    public static partial class TypeMappingService{
        public static string DefaultContainerSuffix="Map";
        public static string ModelMapperAssemblyName=null;
        public static string MapperAssemblyName="ModelMapperAssembly";
        public static string ModelMappersNodeName="ModelMappers";
        public static ConcurrentHashSet<string> ReservedPropertyNames{ get; }=new ConcurrentHashSet<string>();
        public static ConcurrentHashSet<Type> ReservedPropertyTypes{ get; }=new ConcurrentHashSet<Type>();
        public static ConcurrentHashSet<Type> ReservedPropertyInstances{ get; }=new ConcurrentHashSet<Type>();
        public static ConcurrentHashSet<Type> AdditionalTypesList{ get; }=new ConcurrentHashSet<Type>();
        public static ConcurrentHashSet<string> AdditionalReferences{ get; }=new ConcurrentHashSet<string>();
        static ISubject<IModelMapperConfiguration> _typesToMap;
        private static ReplaySubject<IModelMapperConfiguration> _mappingTypes;
        public static ObservableCollection<(string key, Action<(Type declaringType,List<ModelMapperPropertyInfo> propertyInfos)> action)> PropertyMappingRules{ get; private set; }
        public static ObservableCollection<(string key, Action<(Type typeToMap,Result<(string key, string code)> data)> action)> ContainerMappingRules{ get; private set; }
        public static ObservableCollection<(string key, Action<ModelMapperType> action)> TypeMappingRules{ get; private set; }

        static TypeMappingService(){
            Init();
        }

        static void Start(){
            ViewItemService.Connect().Subscribe();
            ConnectCustomizationRules();
            _typesToMap.OnCompleted();
        }


        internal static IObservable<Unit> Connect(){
            return Observable.Return(Unit.Default);
        }

        public static string OutPutAssembly =>
            $@"{Path.GetDirectoryName(typeof(TypeMappingService).Assembly.Location)}\{ModelMapperAssemblyName}{MapperAssemblyName}{ModelExtendingService.Platform}.dll";

        private static void Init(){
            _customizeContainerCode=new Subject<(Type type, Result<(string key, string code)> data)>();
            _customizeProperties =new Subject<(Type declaringType, List<ModelMapperPropertyInfo> propertyInfos)>();
            _customizeTypes =new Subject<ModelMapperType>();
            
            TypeMappingRules = new ObservableCollection<(string key, Action<ModelMapperType> action)>(){
                (nameof(WithNonPublicAttributeParameters), NonPublicAttributeParameters),
                (nameof(GenericTypeArguments), GenericTypeArguments),
            };
            ContainerMappingRules=new ObservableCollection<(string key, Action<(Type typeToMap, Result<(string key, string code)> data)> action)>();
            PropertyMappingRules = new ObservableCollection<(string key, Action<(Type declaringType, List<ModelMapperPropertyInfo> propertyInfos)> action)>{
                (nameof(GenericTypeArguments), GenericTypeArguments),
                (nameof(BrowsableRule), BrowsableRule),
                (nameof(PrivateDescriptionRule), PrivateDescriptionRule),
                (nameof(DefaultValueRule), DefaultValueRule),
                (nameof(WithNonPublicAttributeParameters), NonPublicAttributeParameters),
                (nameof(TypeConverterWithDXDesignTimeType), TypeConverterWithDXDesignTimeType)
            };
            _typesToMap = new ReplaySubject<IModelMapperConfiguration>();
            _mappingTypes = new ReplaySubject<IModelMapperConfiguration>();
            MappingTypes=_mappingTypes;
            MappedTypes = Observable.Defer(() => {
                var distinnctTypesToMap = _typesToMap.Distinct(_ => _.TypeToMap).Do(_mappingTypes);
                return distinnctTypesToMap
                    .All(_ => _.TypeFromPath())
                    .Select(_ => {
                        var assembly = !_? distinnctTypesToMap.ModelCode().Select(tuple => tuple.references.Compile(tuple.code)): Assembly.LoadFile(OutputAssembly).AsObservable();
                        return assembly.SelectMany(assembly1 => {
                            var types = assembly1.GetTypes()
                                .Where(type => typeof(IModelModelMap).IsAssignableFrom(type))
                                .Where(type => !type.Attributes<ModelAbstractClassAttribute>().Any()).ToArray();
                            return types;
                        });
                    }).Switch();
            }).Publish().AutoConnect().Replay().AutoConnect().Distinct();
            _modelMapperModuleVersion = typeof(TypeMappingService).Assembly.GetName().Version;
            
            ReservedPropertyNames.Clear();
            new []{typeof(ModelNode),typeof(IModelNode),typeof(ModelApplicationBase)}
                .SelectMany(_ => _.GetMembers()).Select(_ => _.Name)
                .Concat(new []{"Item","IsReadOnly","Remove","Id","Nodes","IsValid"}).Distinct()
                .ToObservable(Scheduler.Immediate)
                .Do(name => ReservedPropertyNames.Add(name))
                .Subscribe();
            new[]{typeof(Type), typeof(IList), typeof(object), typeof(Array), typeof(IComponent), typeof(ISite)}
                .ToObservable(Scheduler.Immediate)
                .Do(type => ReservedPropertyTypes.Add(type))
                .Subscribe();
            new[]{typeof(IDictionary)}.ToObservable(Scheduler.Immediate)
                .Do(type => ReservedPropertyInstances.Add(type))
                .Subscribe();
            new []{typeof(IModelNode),typeof(DescriptionAttribute),typeof(AssemblyFileVersionAttribute),typeof(ImageNameAttribute),typeof(TypeMappingService)}
                .ToObservable(Scheduler.Immediate)
                .Do(type => AdditionalReferences.Add(type.Assembly.Location))
                .Subscribe();
            
            var systemWebAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetName().Name == "System.Web");
            if (systemWebAssembly != null){
                var type = systemWebAssembly.GetType("System.Web.HttpCookie");
                ReservedPropertyTypes.Add(type);
            }
            ModelExtendingService.Init();
            
        }

        

        public static IEnumerable<Type> ModelMapperContainerTypes(this Type type){
            return type.Assembly.GetTypes()
                .Where(_ => _.Name.EndsWith(DefaultContainerSuffix))
                .Where(_ => _.GetInterfaces().Contains(typeof(IModelModelMapContainer)));
        }

        public static ReplaySubject<IModelMapperConfiguration> MappingTypes{ get;private set; }
        public static IObservable<Type> MappedTypes{ get;private set; }
        

        public static IEnumerable<ModelMapperPropertyInfo> ToModelMapperPropertyInfo(this IEnumerable<PropertyInfo> source){
            return source.Select(_ => _.ToModelMapperPropertyInfo());
        }

        public static ModelMapperPropertyInfo ToModelMapperPropertyInfo(this PropertyInfo propertyInfo){
            return new ModelMapperPropertyInfo(propertyInfo);
        }

        public static IEnumerable<ModelMapperCustomAttributeData> ToModelMapperConfigurationData(this IEnumerable<CustomAttributeData> source){
            return source.Select(_ => new ModelMapperCustomAttributeData(_.AttributeType, _.ConstructorArguments.ToArray()));
        }

        public static IObservable<Type> MapToModel(this IModelMapperConfiguration configurations){
            return new[]{configurations}.ToObservable(Scheduler.Immediate).MapToModel();
        }
        public static IObservable<Type> MapToModel<TModelMapperConfiguration>(this IObservable<TModelMapperConfiguration> configurations)
            where TModelMapperConfiguration : IModelMapperConfiguration{

            //            if (!maps.Contains(PredefinedMap.RepositoryItem) && maps.Any(map =>map.IsRepositoryItem())){
//                maps = maps.Concat(new[]{PredefinedMap.RepositoryItem}).ToArray();
//            }
            return configurations
//                .Select(_ => (_.TypeToMap, (IModelMapperConfiguration) _))
                .Do(_ => _typesToMap.OnNext(_))
                .Select(_ => _.TypeToMap);
        }

        public static IObservable<Type> MapToModel(this IObservable<Type> types,Func<Type,IModelMapperConfiguration> configSelector=null){
            return types.Select(_ => configSelector?.Invoke(_)?? new ModelMapperConfiguration(_)).MapToModel();
        }

        public static IObservable<Type> MapToModel(this Type type,Func<Type,IModelMapperConfiguration> configSelector=null){
            return new[]{type}.MapToModel(configSelector);
        }

        public static IObservable<Type> MapToModel(this IEnumerable<Type> types,Func<Type,IModelMapperConfiguration> configSelector=null){
            return types.ToArray().ToObservable().MapToModel(configSelector);
        }



        public static IObservable<Type> ModelInterfaces(this IModelMapperConfiguration configuration){
            return new[]{configuration}.ToObservable(Scheduler.Immediate).ModelInterfaces();
        }

        public static IObservable<Type> ModelInterfaces(this IObservable<IModelMapperConfiguration> source){
            return source.Select(_ => _.TypeToMap).ModelInterfaces();
        }

        public static IObservable<Type> ModelInterfaces(this IObservable<Type> source){
            return source.Finally(Start).Select(_ => MappedTypes).Switch().Distinct();
        }

        public static string ModelMapContainerName(this Type type, IModelMapperConfiguration configuration=null){
            return configuration?.ContainerName?? $"{type.Name}{DefaultContainerSuffix}";
        }

        public static IModelNode MapNode(this IModelNode modelNode,Type type){
            return modelNode.GetNode(type.Name);
        }

        public static string ModelTypeName(this Type type,Type rootType=null, IModelMapperConfiguration configuration=null){
            if (rootType==null){
                rootType = type;
//                return configuration?.MapName??type.Name;
            }

            return (type, rootType).ModelName(configuration?.MapName);
        }
    }
}