using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reflection;
using Fasterflect;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.ModelMapper.Services{
    class ChartControlSeriesService{
        public static IObservable<Unit> Connect(){
//            TypeMappingService.PropertyMappingRules.Insert(0,(nameof(MapSeries),MapSeries));
            return Unit.Default.AsObservable();
        }

        private static void MapSeries((Type declaringType, List<PropertyInfo> propertyInfos) data){
            if (data.declaringType.FullName == PredifinedMap.ChartControl.GetTypeName()){
                var propertyInfo = data.declaringType.Property("Series");
                data.propertyInfos.Add(propertyInfo);
            }
        }
    }
}