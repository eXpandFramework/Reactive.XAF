using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Fasterflect;
using Mono.Cecil;

namespace Xpand.XAF.Modules.ModelMapper.Services.TypeMapping{
    public static partial class TypeMappingService{
        public static string DefaultContainerSuffix="Map";
        public static string ModelMapperAssemblyName=null;
        public static string MapperAssemblyName="ModelMapperAssembly";
        public static string ModelMappersNodeName="ModelMappers";
        public static List<string> ReservedPropertyNames{ get; }=new List<string>();
        public static List<Type> ReservedPropertyTypes{ get; }=new List<Type>();
        static ISubject<(Type type,IModelMapperConfiguration configuration)> _typesToMap;
        public static List<(string key, Action<CustomizeAttribute> action)> AttributeMappingRules{ get; private set; }

        public static List<(string key, Action<List<PropertyInfo>> action)> PropertyMappingRules{ get; private set; }

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
            PropertyMappingRules = new List<(string key, Action<List<PropertyInfo>> action)>{
                ("Browsable", BrowsableRule)
            };
            AttributeMappingRules = new List<(string key, Action<CustomizeAttribute> action)>{
                ("PrivateDescription", PrivateDescriptionRule),
                ("DefaultValue", DefaultValueRule)
            };
            _typesToMap = Subject.Synchronize(new ReplaySubject<(Type type,IModelMapperConfiguration configuration)>());
            MappedTypes = Observable.Defer(() => {
                var distinnctTypesToMap = Observable.Defer(() => _typesToMap.Distinct(_ => _.type));
                return distinnctTypesToMap
                    .All(_ => _.TypeFromPath())
                    .Select(_ =>!_? distinnctTypesToMap.ModelCode().Compile(): Assembly.LoadFile(OutputAssembly).GetTypes()
                                .Where(type => typeof(IModelModelMap).IsAssignableFrom(type)).ToObservable()).Switch();
            }).Replay().AutoConnect();
            _modelMapperModuleVersion = typeof(TypeMappingService).Assembly.GetName().Version;
            
            ReservedPropertyNames.Clear();
            ReservedPropertyNames.AddRange(typeof(IModelNode).Properties().Select(info => info.Name).Concat(new[]{"Item","IsReadOnly"}));
            ReservedPropertyTypes.AddRange(new[]{ typeof(Type)});
            
            ModelExtendingService.Init();
            
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
            return source.Finally(Start).Select(_ => MappedTypes).Switch();
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

        public static string ModelMapContainerName(this Type type, IModelMapperConfiguration configuration=null){
            return configuration?.ContainerName?? $"{type.Name}{DefaultContainerSuffix}";
        }

        public static string ModelMapName(this Type type,Type rootType=null, IModelMapperConfiguration configuration=null){
            if (rootType==null){
                return configuration?.MapName??type.Name;
            }

            return (type, rootType).ModelName(configuration?.MapName);
        }

        public static void SetArgumentValue(this CustomAttribute customAttribute,
            CustomAttributeArgument argument, object value){

            var arguments = customAttribute.ConstructorArguments;
            arguments.Add(new CustomAttributeArgument(argument.Type, value));
            arguments.Remove(argument);
        }

    }
}