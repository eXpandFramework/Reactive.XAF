using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using Fasterflect;
using Xpand.Source.Extensions.Linq;
using Xpand.Source.Extensions.System.Refelction;
using Xpand.Source.Extensions.XAF.Model;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.ModelMapper.Services{
    public static class ModelBindingService{
        static readonly Subject<object> ControlBoundSubject=new Subject<object>();

        public static IObservable<object> ControlBound => ControlBoundSubject;

        internal static IObservable<Unit> BindConnect(this XafApplication application){
            if (application==null)
                return Observable.Empty<Unit>();
            return application.WhenListViewCreated()
                .Select(_ => _.e.View).Cast<ListView>()
                .ControlsCreated()
                .ViewModelProperties()
                .Select(BindTo);
                
        }

        private static Unit BindTo((PropertyInfo info, IModelNode model, ListView view) _){
            var interfaces = _.model.GetType().GetInterfaces();
            var mapInterface = interfaces.First(type1 => type1.Property(_.info.Name) != null);
            var type = Type.GetType(mapInterface.Attribute<ModelMapLinkAttribute>().LinkedTypeName);
            var control = EnumsNET.Enums.GetMember<PredifinedMap>(type?.Name).Value.GetViewControl(_.view, _.model.Id());
            var modelMap = (IModelModelMap) _.info.GetValue(_.model);
            modelMap.BindTo(control);
            ControlBoundSubject.OnNext(control);
            return Unit.Default;
        }

        private static IObservable<(PropertyInfo info, IModelNode model, ListView view)> ViewModelProperties(this IObservable<(ListView view, EventArgs e)> source){
            return source.SelectMany(_ => _.view.Model.GetType().Properties()
                .Select(info => (info, model: (IModelNode) _.view.Model, _.view)).Concat(_.view.Model.Columns
                    .SelectMany(column =>column.GetType().Properties().Select(info => (info, model: (IModelNode) column, _.view))))
                .Where(info => typeof(IModelModelMap).IsAssignableFrom(info.info.PropertyType)&&_.view.Model.IsPropertyVisible(info.info.Name)));
        }

        public static void BindTo(this IModelModelMap modelModelMap, object instance){
            ((IModelNodeDisabled) modelModelMap).BindTo( instance);
        }

        private static void BindTo(this IModelNodeDisabled modelNodeDisabled, object instance){
            if (!modelNodeDisabled.NodeDisabled){
                var modelNode = ((ModelNode) modelNodeDisabled);
                var modelNodeInfo = modelNode.NodeInfo;
                var propertyInfos = instance.GetType().Properties(Flags.Public|Flags.Static|Flags.AllMembers).DistinctBy(info => info.Name).ToDictionary(info => info.Name,info => info);
                var getValueMethod = modelNode.GetType().Methods(nameof(modelNode.GetValue)).First(info => info.GetGenericArguments().Any());
                var modelValueInfos = modelNodeInfo.ValuesInfo.Where(info => IsValidInfo(info, propertyInfos))
                    .Where(info => !TypeMappingService.ReservedPropertyNames.Contains(info.Name)).ToArray();
                
                foreach (var valueInfo in modelValueInfos){
//                    var propertyType = valueInfo.PropertyType == typeof(string)
//                        ? valueInfo.PropertyType
//                        : valueInfo.PropertyType.GetGenericArguments().First();

//                    var method = getValueMethod.MakeGenericMethod(propertyType);
//                    var type = method.CreateDelegateType();
//                    var delegateForCallMethod = Delegate.CreateDelegate(type,modelNode, method);
//                    var value = delegateForCallMethod.DynamicInvoke(valueInfo.Name);
                    var value = modelNode.GetValue(valueInfo.Name);
                    if (value != null) propertyInfos[valueInfo.Name].SetValue(instance,value);
                }

                for (int i = 0; i < modelNodeDisabled.NodeCount; i++){
                    if (modelNodeDisabled.GetNode(i) is IModelNodeDisabled nodeEnabled){
                        var propertyValue = propertyInfos[nodeEnabled.Id()].GetValue(instance);
                        if (propertyValue != null) (nodeEnabled).BindTo(propertyValue);
                    }
                }
            }
        }

        private static bool IsValidInfo(ModelValueInfo info, Dictionary<string, PropertyInfo> properties){
            return !info.IsReadOnly &&  properties.ContainsKey(info.Name);
        }
    }
}