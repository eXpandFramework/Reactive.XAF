using System;
using System.Reactive;
using DevExpress.ExpressApp.Model;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.ModelMapper.Services.Predefined{
    public class LayoutGroupControlService{
        
        public static IObservable<Unit> Connect(){
            
            TypeMappingService.TypeMappingRules.Add((PredefinedMap.LayoutControlGroup.ToString(), TypeMappingRule));
            return Unit.Default.AsObservable();
        }

        private static void TypeMappingRule(ModelMapperType _){
            var typeToMap = PredefinedMap.LayoutControlGroup.TypeToMap();
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