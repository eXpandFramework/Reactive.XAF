using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Xpand.Source.Extensions.XAF.Model;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services.Predefined;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using Xpand.XAF.Modules.Reactive.Extensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.ModelMapper.Services{
    public class Parameter:HandledEventArgs{
        public Parameter(object item, IModelNode model, ObjectView objectView){
            Item = item;
            Model = model;
            ObjectView = objectView;
        }

        public object Item{ get; }
        public IModelNode Model{ get; }
        public ObjectView ObjectView{ get; }
    }
    public static class ModelBindingService{
        static readonly Subject<Parameter> ControlBindSubject=new Subject<Parameter>();

        public static IObservable<Parameter> ControlBind => ControlBindSubject;

        internal static IObservable<Unit> BindConnect(this XafApplication application){
            
            if (application==null)
                return Observable.Empty<Unit>();
            var controlsCreated = application.WhenObjectViewCreated()
                .ControlsCreated().Select(tuple => tuple).Publish().RefCount();
            var viewItemBindData = controlsCreated.ViewItemData(ViewItemService.RepositoryItemsMapName)
                .Merge(controlsCreated.ViewItemData(ViewItemService.PropertyEditorControlMapName,view => view is DetailView))
                .Select(_ => {
                    var mapType = _.modelMap.GetType().GetInterfaces().First(type =>typeof(IModelModelMap) != type && typeof(IModelModelMap).IsAssignableFrom(type) &&
                        type.Attribute<ModelAbstractClassAttribute>() == null);
                    var controlType = Type.GetType(mapType.Attribute<ModelMapLinkAttribute>().LinkedTypeName);
                    var predefinedMap = EnumsNET.Enums.GetValues<PredefinedMap>().First(map => map.TypeToMap()==controlType);
                    var control = predefinedMap.GetViewControl(_.objectView, _.modelMap.Parent.Parent.Id());
                    return (_.modelMap, control,_.objectView);
                });
            return controlsCreated
                .ViewModelProperties()
                .Select(BindData).WhenNotDefault()
                .Merge(viewItemBindData)
                .Do(tuple => BindTo(tuple))
                .ToUnit();
                
        }

        private static IObservable<(ObjectView objectView, IModelModelMap modelMap)> ViewItemData(this IObservable<(ObjectView view, EventArgs e)> controlsCreated, string propertyMapName,Func<ObjectView,bool> viewMatch=null){
            return controlsCreated.Where(_ => viewMatch?.Invoke(_.view)??true).SelectMany(_ => {
                return _.view.Model.AsObjectView.Items().Cast<IModelNode>()
                    .SelectMany(item => item.GetNode(propertyMapName)?.Nodes().Cast<IModelModelMap>() ?? Enumerable.Empty<IModelModelMap>())

                    .Select(node => (_.view,node));
            });
        }

        private static (IModelModelMap modelMap, object control,ObjectView view) BindData((PropertyInfo info, IModelNode model, ObjectView view) data){
            var interfaces = data.model.GetType().GetInterfaces();
            var mapInterface = interfaces.First(type1 => type1.Property(data.info.Name) != null);
            var type = Type.GetType(mapInterface.Attribute<ModelMapLinkAttribute>().LinkedTypeName);
            var model = data.model.Id();
            var control = EnumsNET.Enums.GetMember<PredefinedMap>(type?.Name).Value.GetViewControl(data.view, model);
            var modelMap = (IModelModelMap) data.info.GetValue(data.model);
            if (control!=null){
                return (modelMap, control,data.view);
            }
            return default;
        }

        private static void BindTo(this (IModelNode model, object control, ObjectView view) data){
            var parameter = new Parameter(data.control,data.model,data.view);
            ControlBindSubject.OnNext(parameter);
            if (!parameter.Handled && parameter.Item != null){
                data.model.BindTo(parameter.Item);
            }
        }

        private static IObservable<(PropertyInfo info, IModelNode model, ObjectView view)> ViewModelProperties(this IObservable<(ObjectView view, EventArgs e)> source){
            return source.SelectMany(_ => {
                var objectView = _.view;
                var items = objectView.Model is IModelListView modelListView
                    ? modelListView.Columns.Concat(new[]{modelListView.SplitLayout}.Cast<IModelNode>())
                    : ((IModelDetailView) objectView.Model).Items.OfType<IModelCommonMemberViewItem>()
                    .Cast<IModelNode>();
                var viewItemData = items.SelectMany(column =>column.ToModelData(objectView));
                var viewData = objectView.Model.ToModelData(objectView);
                
                return viewData.Concat(viewItemData)
                    .Where(info => typeof(IModelModelMap).IsAssignableFrom(info.info.PropertyType))
                    .Where(info => info.model.IsPropertyVisible(info.info.Name));
            });
        }

        public static IEnumerable<(PropertyInfo info, IModelNode model, ObjectView view)> ToModelData(this IModelNode node,ObjectView objectView){
            return node.GetType().Properties().Select(info => (info, model: node,view:objectView));
        }

        public static void BindTo(this IModelNode modelNode, object instance){
            ((IModelNodeDisabled) modelNode).BindTo( instance);
        }

        private static void BindTo(this IModelNodeDisabled modelNodeDisabled, object instance){
            if (!modelNodeDisabled.NodeDisabled){
                var modelNode = ((ModelNode) modelNodeDisabled);
                var modelNodeInfo = modelNode.NodeInfo;
                var propertyInfos = instance.GetType().Properties(Flags.Public|Flags.Static|Flags.AllMembers).DistinctBy(info => info.Name).ToDictionary(info => info.Name,info => info);
                
                var modelPropertyNames = modelNodeInfo.ValuesInfo.Where(info => IsValidInfo(info, propertyInfos))
                    .Where(info => !TypeMappingService.ReservedPropertyNames.Contains(info.Name)).Select(info => info.Name).ToArray();
                
                foreach (var name in modelPropertyNames){
                    var value = modelNode.GetValue(name);
                    if (value != null) propertyInfos[name].SetValue(instance,value);
                }

                for (int i = 0; i < modelNodeDisabled.NodeCount; i++){
                    var node = modelNodeDisabled.GetNode(i);
                    var key = node.Id();
                    if (propertyInfos.TryGetValue(key, out var info)){
                        if (node is IModelNodeDisabled nodeEnabled){
                            var propertyValue = info.GetValue(instance);
                            if (propertyValue != null) (nodeEnabled).BindTo(propertyValue);
                        }
//                        else if (node is IEnumerable enumerable){
//                            foreach (var childNode in enumerable.Cast<IModelNode>()){
//                                childNode
//                            }
//                        }
                    }
                    
                }
            }
        }

        private static bool IsValidInfo(ModelValueInfo info, Dictionary<string, PropertyInfo> properties){
            return !info.IsReadOnly &&  properties.ContainsKey(info.Name);
        }
    }
}