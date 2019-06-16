using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.ModelMapper{
    public static class ModelMapperExtenderService{
        private static HashSet<(Type targetIntefaceType, (Type extenderType, IModelMapperConfiguration configuration) extenderData)>
            ModelExtenders{ get; } =new HashSet<(Type targetIntefaceType, (Type extenderType, IModelMapperConfiguration configuration)extenderType)>();

        static ModelMapperExtenderService(){
            Init();
        }

        internal static void Init(){
            ModelExtenders.Clear();
        }

        internal static IObservable<Unit> Connect(this ApplicationModulesManager applicationModulesManager){
            
            var extendModel = applicationModulesManager.Modules.OfType<ReactiveModule>().ToObservable()
                .SelectMany(module => module.ExtendModel);
            
            return extendModel
                .Select(AddExtenders).Switch()
                .ToUnit();
        }

        private static IObservable<(Type targetIntefaceType, Type extenderInterface)> AddExtenders(ModelInterfaceExtenders extenders){
            var mappedTypes = ModelMapperService.Connect()
                .SelectMany(unit => ModelExtenders.ToObservable()
                    .SelectMany(_ => _.extenderData.MapToModel())
                    .ModelInterfaces()
                    .Where(type => typeof(IModelNode).IsAssignableFrom(type))
                    .Select(_ => _.Assembly.GetTypes().First(type => typeof(IModelModelMapContainer).IsAssignableFrom(type))));

            return ModelExtenders.ToObservable()
                .SelectMany(_ => mappedTypes.Select(extenderInterface => (_.targetIntefaceType,extenderInterface)))
                .Do(_ => extenders.Add(_.targetIntefaceType,_.extenderInterface));
        }

        public static void Extend<TModelMapperConfiguration>(this (Type extenderType, TModelMapperConfiguration configuration) extenderData, Type targetInterface)
            where TModelMapperConfiguration : IModelMapperConfiguration{
            ModelMapperService.Connect().Wait();
            ModelExtenders.Add((targetInterface, extenderData));
        }

        public static void Extend<TTargetInterface>(this Type extenderType,IModelMapperConfiguration configuration ) 
            where TTargetInterface : IModelNode {

            configuration =configuration?? new ModelMapperConfiguration();
            (extenderType,configuration).Extend(typeof(TTargetInterface));
        }

        public static void Extend<TTargetInterface>(this Type extenderType) where TTargetInterface : IModelNode{
            (extenderType,new ModelMapperConfiguration()).Extend(typeof(TTargetInterface));
        }

        public static void Extend<TTargetInterface>(this (Type extenderType, IModelMapperConfiguration configuration) extenderData) where TTargetInterface : IModelNode{
            extenderData.Extend(typeof(TTargetInterface));
        }

        public static void Extend<TTargetInterface, TExtenderType>() where TTargetInterface : IModelNode where TExtenderType:class{
            (typeof(TExtenderType),new ModelMapperConfiguration()).Extend(typeof(TTargetInterface));
        }
    }
}