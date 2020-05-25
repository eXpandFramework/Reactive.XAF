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
using DevExpress.Persistent.Base;
using Fasterflect;
using JetBrains.Annotations;
using Xpand.Extensions.Linq;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.Model;
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

        [PublicAPI]
        public static IObservable<Parameter> ControlBind => ControlBindSubject;

        internal static IObservable<Unit> BindConnect(this XafApplication application){
            
            if (application==null)
                return Observable.Empty<Unit>();
            var controlsCreated = application.WhenObjectViewCreated()
                .ControlsCreated().Select(tuple => tuple)
                .Retry(application)
                .Publish().RefCount();
            
            return controlsCreated.ViewModelProperties()
                .Merge(controlsCreated.ViewItemBindData())
                .Do(tuple => BindTo(tuple))
                .TraceModelMapper()
                .ToUnit()
                .Merge(application.BindLayoutGroupControl().TraceModelMapper().ToUnit())
                .Retry(application);
                
        }

        private static IObservable<(IModelModelMap modelMap, object control, ObjectView objectView)> ViewItemBindData(this IObservable<ObjectView> controlsCreated){
            var viewItemBindData = controlsCreated.ViewItemData(ViewItemService.RepositoryItemsMapName)
                .Merge(controlsCreated.ViewItemData(ViewItemService.PropertyEditorControlMapName, view => view is DetailView))
                .Select(_ => {
                    var mapType = _.modelMap.GetType().GetInterfaces().First(type =>
                        typeof(IModelModelMap) != type && typeof(IModelModelMap).IsAssignableFrom(type) &&
                        type.Attribute<ModelAbstractClassAttribute>() == null);
                    var controlType = Type.GetType(mapType.Attribute<ModelMapLinkAttribute>().LinkedTypeName);
                    var predefinedMap = EnumsNET.Enums.GetValues<PredefinedMap>().First(map => map.TypeToMap() == controlType);
                    var control = predefinedMap.GetViewControl(_.objectView, _.modelMap.Parent.Parent.Id());
                    return (_.modelMap, control, _.objectView);
                });
            return viewItemBindData.TraceModelMapper(_ => $"{_.control.GetType().Name}, {_.modelMap.Id()}, {_.objectView.Id}");
        }

        private static IObservable<(IModelModelMap modelMap, object control, ObjectView view)> BindLayoutGroupControl(this XafApplication application){
            var bindLayoutGroupControl = application.WhenDetailViewCreated().ToDetailView()
                .SelectMany(_ => _.LayoutManager.WhenCustomizeAppearence().Select(pattern => (view: _, pattern.EventArgs)))
                .Where(_ => {
                    var item = _.EventArgs.Item.GetPropertyValue("Item");
                    if (item.GetType().Name != "XafLayoutControlGroup") return false;
                    var model = (IModelNode) item.GetPropertyValue("Model");
                    var showCaption = model.GetValue<bool?>("ShowCaption");
                    return showCaption != null && (bool) showCaption;
                })
                .SelectMany(_ => {
                    var control = _.EventArgs.Item.GetPropertyValue("Item");
                    return ((IModelNode) control.GetPropertyValue("Model")).ToBindableData(_.view)
                        .Select(tuple => BindData(tuple,control)).Where(tuple => tuple.modelMap!=null);
                })
                .TraceModelMapper(_ => $"{_.control.GetType().Name}, {_.view.Id}, {_.modelMap.Id()}")
                .Do(tuple => tuple.BindTo());
            return bindLayoutGroupControl;
        }

        private static IObservable<(ObjectView objectView, IModelModelMap modelMap)> ViewItemData(
            this IObservable<ObjectView> controlsCreated, string propertyMapName,Func<ObjectView, bool> viewMatch = null){

            return controlsCreated.Where(objectView => viewMatch?.Invoke(objectView)??true).SelectMany(objectView => {
                return objectView.Model.AsObjectView.CommonMemberViewItems().Cast<IModelNode>()
                    .SelectMany(item => item.GetNode(propertyMapName)?.Nodes().Cast<IModelModelMap>() ?? Enumerable.Empty<IModelModelMap>())
                    .Select(node => (objectView,node));
            });
        }

        private static (IModelModelMap modelMap, object control,ObjectView view) BindData((PropertyInfo info, IModelNode model, ObjectView view) data,object control=null){
            var modelMap=data.model as IModelModelMap;
            if (control == null){
                var interfaces = data.model.GetType().GetInterfaces();
                var infoName = data.info.Name;
                var mapInterface = interfaces.First(type1 => type1.Property(infoName) != null);
                var type = Type.GetType(mapInterface.Attribute<ModelMapLinkAttribute>().LinkedTypeName);
                var model = data.model.Id();
                control = EnumsNET.Enums.GetMember<PredefinedMap>(type?.Name!)?.Value.GetViewControl(data.view, model);
                modelMap = data.info.GetValue(data.model) as IModelModelMap;
            }
            if (control!=null){
                return (modelMap, control,data.view);
            }
            return default;
        }

        private static void BindTo(this (IModelNodeDisabled model, object control, ObjectView view) data){
            var parameter = new Parameter(data.control,data.model,data.view);
            ControlBindSubject.OnNext(parameter);
            if (!parameter.Handled && parameter.Item != null){
                data.model.BindTo(parameter.Item);
            }
        }

        internal static IObservable<(IModelModelMap modelMap, object control, ObjectView view)> ViewModelProperties(this IObservable<ObjectView> source){
            return source.SelectMany(objectView => {
                    var items = objectView.Model is IModelListView modelListView
                    ? modelListView.Columns.Concat(new[]{modelListView.SplitLayout}.Cast<IModelNode>())
                    : ((IModelDetailView) objectView.Model).Items.OfType<IModelCommonMemberViewItem>()
                    .Cast<IModelNode>();
                var viewItemData = items.SelectMany(column =>column.ToBindableData(objectView));
                var viewData = objectView.Model.ToBindableData(objectView);
                return viewData.Concat(viewItemData);
            })
            .Select(_ => BindData(_))
            .WhenNotDefault()
            .TraceModelMapper(_ =>$"{_.control.GetType().Name}, {_.view.Id}, {_.modelMap.Id()}" );
        }

        private static IEnumerable<(PropertyInfo info, IModelNode model, ObjectView view)> ToBindableData(this IModelNode node,ObjectView objectView){
            if (node is IModelModelMappersContextDependency contextDependency){
                var modelMappers = contextDependency.ModelMapperContext;
                return modelMappers != null ? modelMappers.Select(_ => ((PropertyInfo) null, (IModelNode) _, objectView))
                    : Enumerable.Empty<(PropertyInfo info, IModelNode model, ObjectView view)>();
            }
            return node.GetType().Properties()
                .Where(info => typeof(IModelModelMap).IsAssignableFrom(info.PropertyType))
                .Where(info => node.IsPropertyVisible(info.Name))
                .Select(info => (info, model: node,view:objectView));
        }

        public static void BindTo(this IModelNodeDisabled modelNode, object instance){
            modelNode.BindToModel( instance);
        }

        private static void BindToModel(this IModelNodeDisabled modelNodeDisabled, object instance){
            if (!modelNodeDisabled.NodeDisabled){
                var modelNode = ((ModelNode) modelNodeDisabled);
                var modelNodeInfo = modelNode.NodeInfo;
                var propertyInfos = instance.BindablePropertyInfos(modelNodeDisabled);
                
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
                            if (propertyValue != null) (nodeEnabled).BindToModel(propertyValue);
                        }
                    }
                    
                }
            }
        }

        private static Dictionary<string, PropertyInfo> BindablePropertyInfos(this object instance,IModelNodeDisabled modelNodeDisabled){
            try{
                return instance.GetType().Properties(Flags.Public|Flags.Static|Flags.AllMembers).DistinctBy(info => info.Name).ToDictionary(info => info.Name,info => info);
            }
            catch (Exception e){
                var exception = new Exception($"ModelNode:{((ModelNode) modelNodeDisabled).Path}, object:{instance}",e);
                Tracing.Tracer.LogError(exception);
                return new Dictionary<string, PropertyInfo>();
            }
        }

        private static bool IsValidInfo(ModelValueInfo info, Dictionary<string, PropertyInfo> properties){
            return !info.IsReadOnly &&  properties.ContainsKey(info.Name);
        }
    }
}