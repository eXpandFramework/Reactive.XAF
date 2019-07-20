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
using Xpand.XAF.Modules.ModelMapper.Configuration;
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

        private static HashSet<IModelMapperConfiguration> ModelMapperConfigurations{ get; } =new HashSet<IModelMapperConfiguration>();

        static ModelExtendingService(){
            Init();
        }

        internal static void Init(){
            ModelMapperConfigurations.Clear();
        }

        internal static IObservable<Unit> ConnectExtendingService(this ApplicationModulesManager applicationModulesManager){
            Platform = applicationModulesManager.Modules.GetPlatform();
            ConnectedSubject.OnNext(Unit.Default);
            var extendModel = applicationModulesManager.Modules.OfType<ReactiveModule>().ToObservable().FirstAsync()
                .Select(module => module.ExtendModel).Switch().FirstAsync();
            
            return extendModel.Select(extenders => extenders)
                .Select(AddExtenders).Switch()
                .ToUnit();
        }

        private static IObservable<(Type targetIntefaceType, Type extenderInterface)> AddExtenders(ModelInterfaceExtenders extenders){
            var modelExtenders = ModelMapperConfigurations.Distinct(_ => _.TypeToMap).ToObservable();
            var mappedContainers = TypeMappingService.Connect()
                .SelectMany(unit => modelExtenders
                    .Select(_ => _.MapToModel().Select(type => type)).Switch()
                    .ModelInterfaces()
                    .Where(type => typeof(IModelNode).IsAssignableFrom(type)))
                .SelectMany(type => type.ModelMapperContainerTypes())
                .Distinct().Replay().RefCount();

            return modelExtenders
                .SelectMany(_ => mappedContainers.FirstAsync(type =>
                        type.Attribute<ModelMapLinkAttribute>().LinkedTypeName == _.TypeToMap.AssemblyQualifiedName)
                    .SelectMany(extenderInterface => _.TargetInterfaceTypes.Select(targetInterfaceType => (targetInterfaceType, extenderInterface))))
                .Do(_ => extenders.Add(_.targetInterfaceType,_.extenderInterface));
        }


        public static void Extend(this ApplicationModulesManager modulesManager, IModelMapperConfiguration configuration){
            if (modulesManager.Modules.FindModule<ModelMapperModule>()==null)
                throw new NotSupportedException($"{typeof(ModelMapperModule)} is not registered");
            TypeMappingService.Connect().Wait();
            ModelMapperConfigurations.Add(configuration);

        }
        public static void Extend<TTargetInterface>(this ApplicationModulesManager modulesManager,Type extenderType) 
            where TTargetInterface : IModelNode {

            
            modulesManager.Extend(new ModelMapperConfiguration(extenderType,typeof(TTargetInterface)));
        }


        public static void Extend<TTargetInterface, TExtenderType>(this ApplicationModulesManager modulesManager) where TTargetInterface : IModelNode where TExtenderType:class{
            modulesManager.Extend(new ModelMapperConfiguration(typeof(TExtenderType),typeof(TTargetInterface)));
        }
    }
}