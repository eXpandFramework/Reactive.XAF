using System;
using System.Reactive;
using DevExpress.ExpressApp.Model;
using Xpand.Extensions.EventArgExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;

namespace Xpand.XAF.Modules.ModelMapper.Services.Predefined{
    public static class LayoutGroupControlService{
        
        public static IObservable<Unit> Connect(){
            
            TypeMappingService.TypeMappingRules.Add((PredefinedMap.LayoutControlGroup.ToString(), TypeMappingRule));
            return Unit.Default.Observe();
        }

        private static void TypeMappingRule(GenericEventArgs<ModelMapperType> e){
            var typeToMap = PredefinedMap.LayoutControlGroup.TypeToMap();
            var _ = e.Instance;
            if (_.Type == typeToMap){
                if (_.TypeToMap == null){
                    _.BaseTypeFullNames.Add(typeof(IModelLayoutGroup).FullName);
                }
                else if (_.TypeToMap == typeToMap){
                    _.BaseTypeFullNames.Add(typeof(IModelViewLayoutElement).FullName);
                }
            }
        }
    }
}