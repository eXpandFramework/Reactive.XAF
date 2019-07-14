using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reflection;
using DevExpress.Utils.Extensions;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.ModelMapper.Services.Predifined{
    class SchedulerControlService{
        public static IObservable<Unit> Connect(Type typeToMap, Assembly schedulerCoreAssembly){
            var storageData = new[]{(property:"Labels",typeName:"AppointmentLabel",assembly:typeToMap.Assembly),(property:"Mappings",typeName:"ResourceMappingInfo",assembly:schedulerCoreAssembly)};
            var types = storageData
                .Select(_ => (_.property,listType:typeof(IList<>).MakeGenericType(_.assembly.GetType($"DevExpress.XtraScheduler.{_.typeName}"))))
                .ToArray();
            TypeMappingService.AdditionalTypesList.AddRange(types.Select(_ => _.listType));
            TypeMappingService.PropertyMappingRules.Insert(0,(PredifinedMap.SchedulerControl.ToString(),data => SchedulerStorage(data,typeToMap, types)));
            return Unit.Default.AsObservable();
        }


        private static void SchedulerStorage((Type declaringType, List<ModelMapperPropertyInfo> propertyInfos) data,Type typeToMap, (string property, Type listType)[] propertyData){
            if (data.declaringType == typeToMap){
                var propertyInfo = data.propertyInfos.First(info => info.Name=="Storage");
                propertyInfo.RemoveAttribute(typeof(BrowsableAttribute));
            }
            else if (data.declaringType.FullName == "DevExpress.XtraScheduler.AppointmentStorage"){
                foreach (var pData in propertyData){
                    var propertyInfo = data.propertyInfos.First(info => info.Name==pData.property);
                    data.propertyInfos.Remove(propertyInfo);
                    var modelMapperPropertyInfo = new ModelMapperPropertyInfo(pData.property,pData.listType,propertyInfo.DeclaringType);
                    data.propertyInfos.Add(modelMapperPropertyInfo);    
                }
            }

        }
    }
}