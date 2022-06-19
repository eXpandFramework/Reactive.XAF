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
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.ReflectionExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services.Predefined;

namespace Xpand.XAF.Modules.ModelMapper.Services.TypeMapping{
    public static partial class TypeMappingService{
        public static string DefaultContainerSuffix="Map";
        public static string ModelMapperAssemblyName;
        public static string MapperAssemblyName="ModelMapperAssembly";
        public static string ModelMappersNodeName="ModelMappers";

        internal static string OutputAssembly;

        public static ConcurrentHashSet<string> ReservedPropertyNames{ get; }=new();
        public static ConcurrentHashSet<Type> ReservedPropertyTypes{ get; }=new();
        public static ConcurrentHashSet<Type> ReservedPropertyInstances{ get; }=new();
        public static ConcurrentHashSet<Type> AdditionalTypesList{ get; }=new();
        public static ConcurrentHashSet<string> AdditionalReferences{ get; }=new();
        static ISubject<IModelMapperConfiguration> _typesToMap;
        
        public static ObservableCollection<(string key, Action<(Type declaringType,List<ModelMapperPropertyInfo> propertyInfos)> action)> PropertyMappingRules{ get; private set; }
        public static ObservableCollection<(string key, Action<(Type typeToMap,Result<(string key, string code)> data)> action)> ContainerMappingRules{ get; private set; }
        public static ObservableCollection<(string key, Action<GenericEventArgs<ModelMapperType>> action)> TypeMappingRules{ get; private set; }

        static TypeMappingService(){
            Init();
        }

        static void Start(){
            
            ViewItemService.Connect().Subscribe();
            ConnectCustomizationRules();
            _typesToMap.OnCompleted();
        }

        internal static IObservable<Unit> Connect() 
            => Observable.Return(Unit.Default).TraceModelMapper();

        internal static void Init(){
            var tempPath = $@"{Path.GetTempPath()}\{nameof(ModelMapperModule)}";
            if (!Directory.Exists(tempPath)){
                Directory.CreateDirectory(tempPath);
            }
            var path = DesignerOnlyCalculator.IsRunFromDesigner?tempPath:AppDomain.CurrentDomain.ApplicationPath();
            OutputAssembly = $@"{path}\{MapperAssemblyName}{ModelMapperAssemblyName}{{0}}.dll";
            _customizeContainerCode=new Subject<(Type type, Result<(string key, string code)> data)>();
            _customizeProperties =new Subject<(Type declaringType, List<ModelMapperPropertyInfo> propertyInfos)>();
            _customizeTypes =new Subject<GenericEventArgs<ModelMapperType>>();
            
            TypeMappingRules = GetTypeMappingRules();
            ContainerMappingRules=new ObservableCollection<(string key, Action<(Type typeToMap, Result<(string key, string code)> data)> action)>();
            PropertyMappingRules = GetPropertyMappingRules();
            _typesToMap = new ReplaySubject<IModelMapperConfiguration>();
            
            MappingTypes=new ReplaySubject<IModelMapperConfiguration>();
            
            MappedTypes = GetMappedTypes().Replay().RefCount();
            _modelMapperModuleVersion = typeof(TypeMappingService).Assembly.GetName().Version;
            
            ConfigureReservedProperties();
            ConfigureReservedPropertyTypes();
            ConfigureReservedPropertyInstances();
            ConfigureAdditionalReferences();
        }

        private static Type[] PredefinedMaps() 
            => EnumsNET.Enums.GetValues<PredefinedMap>().Select(map => map.TypeToMap())
                .Concat(new[] {typeof(RepositoryItemBaseMap), typeof(PropertyEditorControlMap)}).ToArray();

        private static IObservable<Type> GetMappedTypes() 
            => Observable.Defer(() => _typesToMap.Distinct(_ => _.TypeToMap).Do(configuration => MappingTypes.OnNext(configuration))
                    .BufferUntilCompleted()
                    .SelectMany(_ => {
                        var predefinedMaps = PredefinedMaps();
                        return _typesToMap.Distinct(configuration => configuration.TypeToMap).Select(configuration => (configuration,predefinedMaps));
                    })
                    .GroupBy(t => t.predefinedMaps.Contains(t.configuration.TypeToMap))
                    .SelectMany(obs => obs.Select(t => t.configuration).BufferUntilCompleted()
                        .SelectMany(configurations => {
                            var skipCompilation = configurations
                                .All(configuration =>_skipAssemblyValidation || configuration.TypeFromPath(!obs.Key) );
                            return configurations.ToNowObservable().GetMappedTypes(skipCompilation, !obs.Key);
                        })))
                .Finally(() => _skipAssemblyValidation=false);

        private static IObservable<Type> GetMappedTypes(this IObservable<IModelMapperConfiguration> distinctTypesToMap,bool skipCompilation,bool isCustom) 
            => (!skipCompilation ? distinctTypesToMap.ModelCode().SelectMany(t => t.references.Compile(t.code,isCustom))
                : AppDomain.CurrentDomain.LoadAssembly(GetLastAssemblyPath(isCustom)).ReturnObservable())
                .SelectMany(assembly1 => assembly1.GetTypes()
                    .Where(type => typeof(IModelModelMap).IsAssignableFrom(type))
                    .Where(type => !type.Attributes<ModelAbstractClassAttribute>().Any())
                    .Select(type => type)
                    .ToArray());

        private static void ConfigureAdditionalReferences() 
            => new[]{
                    typeof(IModelNode), typeof(DescriptionAttribute), typeof(AssemblyFileVersionAttribute),
                    typeof(ImageNameAttribute), typeof(TypeMappingService)
                }
                .ToObservable(Scheduler.Immediate)
                .Do(type => AdditionalReferences.Add(type.Assembly.Location))
                .Subscribe();

        private static void ConfigureReservedPropertyInstances() 
            => new[]{typeof(IDictionary)}.ToObservable(Scheduler.Immediate)
                .Do(type => ReservedPropertyInstances.Add(type))
                .Subscribe();

        private static void ConfigureReservedPropertyTypes(){
            new[]{typeof(Type), typeof(IList), typeof(object), typeof(Array), typeof(IComponent), typeof(ISite)}
                .ToObservable(Scheduler.Immediate)
                .Do(type => ReservedPropertyTypes.Add(type))
                .Subscribe();
            var systemWebAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetName().Name == "System.Web");
            if (systemWebAssembly != null){
                var type = systemWebAssembly.GetType("System.Web.HttpCookie");
                ReservedPropertyTypes.Add(type);
            }
        }

        private static void ConfigureReservedProperties(){
            ReservedPropertyNames.Clear();
            new[]{typeof(ModelNode), typeof(IModelNode), typeof(ModelApplicationBase)}
                .SelectMany(_ => _.Members(MemberTypes.Property | MemberTypes.Method)
                    .Where(info => info is MethodBase method
                        ? !method.IsPrivate
                        : new []{AccessModifier.Public,AccessModifier.Protected}
                            .Contains(((PropertyInfo) info).AccessModifier()))).Select(_ => _.Name)
                .Concat(new[]{"Item", "IsReadOnly", "Remove", "Id", "Nodes", "IsValid"}).Distinct()
                .ToObservable(Scheduler.Immediate)
                .Do(name => ReservedPropertyNames.Add(name))
                .Subscribe();
        }

        private static ObservableCollection<(string key, Action<(Type declaringType, List<ModelMapperPropertyInfo> propertyInfos)> action)> GetPropertyMappingRules() 
            => new() {
                (nameof(DesignerSerializationVisibilityAttribute), DesignerSerializationVisibilityAttribute),
                (nameof(GenericTypeArguments), GenericTypeArguments),
                (nameof(BrowsableRule), BrowsableRule),
                (nameof(PrivateDescriptionRule), PrivateDescriptionRule),
                (nameof(DefaultValueRule), DefaultValueRule),
                (nameof(WithNonPublicAttributeParameters), NonPublicAttributeParameters),
                (nameof(TypeConverterWithDXDesignTimeType), TypeConverterWithDXDesignTimeType),
                (nameof(CompilerIsReadOnly), CompilerIsReadOnly),
                (nameof(GenericTypeRule), GenericTypeRule),
            };

        private static ObservableCollection<(string key, Action<GenericEventArgs<ModelMapperType>> action)> GetTypeMappingRules() 
            => new(){
                (nameof(WithNonPublicAttributeParameters), NonPublicAttributeParameters),
                (nameof(GenericTypeArguments), GenericTypeArguments),
                (nameof(CompilerIsReadOnly), CompilerIsReadOnly),
                (nameof(GenericTypeRule), GenericTypeRule)
            };


        public static IEnumerable<Type> ModelMapperContainerTypes(this Type type) 
            => type.Assembly.GetTypes()
                .Where(_ => _.Name.EndsWith(DefaultContainerSuffix))
                .Where(_ => _.GetInterfaces().Contains(typeof(IModelModelMapContainer)));

        public static ReplaySubject<IModelMapperConfiguration> MappingTypes{ get;private set; }
        public static IObservable<Type> MappedTypes{ get;private set; }
        

        public static IEnumerable<ModelMapperPropertyInfo> ToModelMapperPropertyInfo(this IEnumerable<PropertyInfo> source) 
            => source.Select(_ => _.ToModelMapperPropertyInfo());

        public static ModelMapperPropertyInfo ToModelMapperPropertyInfo(this PropertyInfo propertyInfo) 
            => new(propertyInfo);

        public static IEnumerable<ModelMapperCustomAttributeData> ToModelMapperConfigurationData(this IEnumerable<CustomAttributeData> source) 
            => source.Select(_ => new ModelMapperCustomAttributeData(_.AttributeType, _.ConstructorArguments.ToArray()));

        public static IObservable<Type> MapToModel(this IModelMapperConfiguration configurations) 
            => new[]{configurations}.ToObservable(Scheduler.Immediate).MapToModel();

        public static IObservable<Type> MapToModel<TModelMapperConfiguration>(this IObservable<TModelMapperConfiguration> configurations)
            where TModelMapperConfiguration : IModelMapperConfiguration 
            => configurations
                .Do(_ => _typesToMap.OnNext(_))
                .Select(_ => _.TypeToMap)
                .TraceModelMapper(type => type.FullName);

        public static IObservable<Type> MapToModel(this IObservable<Type> types,Func<Type,IModelMapperConfiguration> configSelector=null) 
            => types.Select(_ => configSelector?.Invoke(_)?? new ModelMapperConfiguration(_)).MapToModel();

        public static IObservable<Type> MapToModel(this Type type,Func<Type,IModelMapperConfiguration> configSelector=null) 
            => new[]{type}.MapToModel(configSelector);

        public static IObservable<Type> MapToModel(this IEnumerable<Type> types,Func<Type,IModelMapperConfiguration> configSelector=null) 
            => types.ToArray().ToObservable().MapToModel(configSelector);

        internal static IObservable<Type> ModelInterfaces(this IModelMapperConfiguration configuration) 
            => new[]{configuration}.ToObservable(Scheduler.Immediate).ModelInterfaces();

        internal static IObservable<Type> ModelInterfaces(this IObservable<IModelMapperConfiguration> source) 
            => source.Select(_ => _.TypeToMap).ModelInterfaces();

        internal static IObservable<Type> ModelInterfaces(this IObservable<Type> source) 
            => source.Finally(Start).Select(_ => MappedTypes).Switch().Distinct().TraceModelMapper(type => type.FullName );

        public static string ModelMapContainerName(this Type type, IModelMapperConfiguration configuration=null) 
            => configuration?.ContainerName?? $"{type.Name}{DefaultContainerSuffix}";

        public static IModelNode MapNode(this IModelNode modelNode,Type type) 
            => modelNode.GetNode(type.Name);

        public static Type ModelType(this Type type, Type rootType = null,IModelMapperConfiguration configuration = null) 
            => DevExpress.ExpressApp.XafTypesInfo.Instance.FindTypeInfo(type.ModelTypeName(rootType, configuration)).Type;

        public static string ModelTypeName(this Type type,Type rootType=null, IModelMapperConfiguration configuration=null) {
            rootType ??= type;

            return (type, rootType).ModelName(configuration?.MapName);
        }

        public static void Reset(bool skipAssemblyValidation=false,Platform? platform=null){
            MappedTypes=Observable.Empty<Type>();
            _skipAssemblyValidation = skipAssemblyValidation;
            ContainerMappingRules.Clear();
            AdditionalTypesList.Clear();
            AdditionalReferences.Clear();
            TypeMappingRules.Clear();
            PropertyMappingRules.Clear();
            if (platform != null) ModelExtendingService.Platform = platform.Value;
            Init();
            PredefinedMapService.Init();
            OutputAssembly = $@"{AppDomain.CurrentDomain.ApplicationPath()}\{Path.GetFileNameWithoutExtension(OutputAssembly)}.dll";
            _modelMapperModuleVersion = typeof(ModelMapperModule).Assembly.GetName().Version;
            
        }
    }
}