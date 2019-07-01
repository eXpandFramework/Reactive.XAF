using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reflection;
using Fasterflect;
using Xpand.Source.Extensions.System.Refelction;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.ModelMapper.Services.TypeMapping{
    class BandedGridColumnService{
        private static void BandedGridColumn(List<PropertyInfo> propertyInfos){
            var typeName = PredifinedMap.BandedGridColumn.GetTypeName();
            var propertyNames = new HashSet<string>(new[]{"ColVIndex", "RowIndex"});
            var infos = propertyInfos.Where(info =>
                propertyNames.Contains(info.Name) && info.DeclaringType?.FullName == typeName &&
                info.Attributes<BrowsableAttribute>().Any(_ => !_.Browsable)).ToArray();
            foreach (var propertyInfo in infos){
                propertyInfos.Remove(propertyInfo);
                var dynamicPropertyInfo = new DynamicPropertyInfo(propertyInfo);
                dynamicPropertyInfo.RemoveAttribute(new BrowsableAttribute(false));
                dynamicPropertyInfo.AddAttribute(new CategoryAttribute("Appearance"));
                propertyInfos.Add(dynamicPropertyInfo);
            }
        }

        public static IObservable<Unit> Connect(){
            TypeMappingService.PropertyMappingRules.Insert(0,(nameof(BandedGridColumn),BandedGridColumn));
            return Unit.Default.AsObservable();
        }


    }
}