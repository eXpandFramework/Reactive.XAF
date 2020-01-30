using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ConcurrentCollections;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Fasterflect;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.XafApplication;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using Xpand.XAF.Modules.Reactive;
using TypeMappingService = Xpand.XAF.Modules.ModelMapper.Services.TypeMapping.TypeMappingService;

namespace Xpand.XAF.Modules.ModelMapper.Services{
    
    public static class ModelExtendingService{
        private static readonly ISubject<Unit> ConnectedSubject=Subject.Synchronize(new Subject<Unit>());
        internal static Platform Platform{ get; set; }

        public static IObservable<Unit> Connected => Observable.Defer(() => ConnectedSubject);

        private static ConcurrentHashSet<IModelMapperConfiguration> ModelMapperConfigurations{ get; } =new ConcurrentHashSet<IModelMapperConfiguration>();

        internal static IObservable<Unit> ConnectExtendingService(this ApplicationModulesManager applicationModulesManager){
            
            Platform = applicationModulesManager.Modules.GetPlatform();
            
            var extendModel = applicationModulesManager.Modules.OfType<ReactiveModule>().ToObservable().FirstAsync()
                .Select(module => module.ExtendModel).Switch().FirstAsync();
            
            return extendModel.Select(extenders => extenders)
                .Select(AddExtenders).Switch()
                .Finally(() => {
                    ConnectedSubject.OnNext(Unit.Default);
                    ModelMapperConfigurations.Clear();
                })
                .ToUnit();
        }

        private static IObservable<(Type targetIntefaceType, Type extenderInterface)> AddExtenders(ModelInterfaceExtenders extenders){
            var modelExtenders = ModelExtenders().SubscribeReplay();
            var mappedContainers = TypeMappingService.Connect()
                .SelectMany(unit => modelExtenders
                    .Select(_ => _.MapToModel()).Switch()
                    .ModelInterfaces()
                    .Where(type => typeof(IModelNode).IsAssignableFrom(type)))
                .SelectMany(type => type.ModelMapperContainerTypes())
                .Distinct().Replay().RefCount();

            return modelExtenders
                .SelectMany(_ => mappedContainers.FirstAsync(type =>type.Attribute<ModelMapLinkAttribute>().LinkedTypeName == _.TypeToMap.AssemblyQualifiedName)
                    .SelectMany(extenderInterface => _.TargetInterfaceTypes.Select(targetInterfaceType => (targetInterfaceType, extenderInterface))))
                .Do(_ => extenders.Add(_.targetInterfaceType,_.extenderInterface));
        }

        private static IObservable<IModelMapperConfiguration> ModelExtenders(){
            return ModelMapperConfigurations.Distinct(_ => _.TypeToMap).ToObservable().TraceModelMapper();
        }

        public static void Extend(this ApplicationModulesManager modulesManager, IModelMapperConfiguration configuration){
            if (modulesManager.Modules.FindModule<ModelMapperModule>()==null)
                throw new NotSupportedException($"{typeof(ModelMapperModule)} is not registered");
            TypeMappingService.Connect().Wait();
            var installed = ModelMapperConfigurations.FirstOrDefault(_ => _.TypeToMap==configuration.TypeToMap);
            if (installed != null){
                installed.TargetInterfaceTypes.AddRange(configuration.TargetInterfaceTypes);
            }
            else{
                ModelMapperConfigurations.Add(configuration);
            }
            

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