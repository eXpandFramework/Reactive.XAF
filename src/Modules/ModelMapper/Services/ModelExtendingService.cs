using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Fasterflect;
using Xpand.Source.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;
using TypeMappingService = Xpand.XAF.Modules.ModelMapper.Services.TypeMapping.TypeMappingService;

namespace Xpand.XAF.Modules.ModelMapper.Services{
    
    public class PredifinedMapAttribute:Attribute{
        public PredifinedMapAttribute(PredifinedMap map){
            Map = map;
        }

        public PredifinedMap Map{ get; }
    }
    public static class ModelExtendingService{
        private static readonly Subject<Unit> ConnectedSubject=new Subject<Unit>();
        internal static Platform Platform{ get; private set; }

        private static HashSet<(Type targetIntefaceType, (Type extenderType, IModelMapperConfiguration configuration) extenderData)>
            ModelExtenders{ get; } =new HashSet<(Type targetIntefaceType, (Type extenderType, IModelMapperConfiguration configuration)extenderType)>();

        static ModelExtendingService(){
            Init();
        }

        internal static void Init(){
            ModelExtenders.Clear();
        }

        internal static IObservable<Unit> ConnectExtendingService(this ApplicationModulesManager applicationModulesManager){
            Platform = applicationModulesManager.Modules.GetPlatform();
            ConnectedSubject.OnNext(Unit.Default);
            var extendModel = applicationModulesManager.Modules.OfType<ReactiveModule>().ToObservable()
                .SelectMany(module => module.ExtendModel);
            
            return extendModel
                .Select(AddExtenders).Switch()
                .ToUnit();
        }

        private static IObservable<(Type targetIntefaceType, Type extenderInterface)> AddExtenders(ModelInterfaceExtenders extenders){
            var mappedContainers = TypeMappingService.Connect()
                .SelectMany(unit => ModelExtenders.ToObservable()
                    .SelectMany(_ => _.extenderData.MapToModel())
                    .ModelInterfaces()
                    .Where(type => typeof(IModelNode).IsAssignableFrom(type)))
                .SelectMany(type => type.ModelMapperContainerType())
                .Distinct().Replay().RefCount();

            return ModelExtenders.ToObservable()
                .SelectMany(_ => mappedContainers.FirstAsync(type =>
                        type.Attribute<ModelMapLinkAttribute>().LinkedTypeName == _.extenderData.extenderType.AssemblyQualifiedName)
                    .Select(extenderInterface => (_.targetIntefaceType, extenderInterface)))
                .Do(_ => extenders.Add(_.targetIntefaceType,_.extenderInterface));
        }

        public static void Extend<TModelMapperConfiguration>(this ApplicationModulesManager modulesManager,(Type extenderType, TModelMapperConfiguration configuration) extenderData, Type targetInterface)
            where TModelMapperConfiguration : IModelMapperConfiguration{
            if (modulesManager.Modules.FindModule<ModelMapperModule>()==null)
                throw new NotSupportedException($"{typeof(ModelMapperModule)} is not registered");
            TypeMappingService.Connect().Wait();
            ModelExtenders.Add((targetInterface, extenderData));
        }

        public static void Extend<TTargetInterface>(this ApplicationModulesManager modulesManager,Type extenderType,IModelMapperConfiguration configuration ) 
            where TTargetInterface : IModelNode {

            configuration =configuration?? new ModelMapperConfiguration();
            modulesManager.Extend((extenderType,configuration),typeof(TTargetInterface));
        }

        public static void Extend<TTargetInterface>(this ApplicationModulesManager modulesManager,Type extenderType) where TTargetInterface : IModelNode{
            modulesManager.Extend((extenderType,new ModelMapperConfiguration()),typeof(TTargetInterface));
        }

        public static void Extend<TTargetInterface>(this ApplicationModulesManager modulesManager,(Type extenderType, IModelMapperConfiguration configuration) extenderData) where TTargetInterface : IModelNode{
            modulesManager.Extend(extenderData,typeof(TTargetInterface));
        }

        public static void Extend<TTargetInterface, TExtenderType>(this ApplicationModulesManager modulesManager) where TTargetInterface : IModelNode where TExtenderType:class{
            modulesManager.Extend((typeof(TExtenderType),new ModelMapperConfiguration()),typeof(TTargetInterface));
        }
    }
}