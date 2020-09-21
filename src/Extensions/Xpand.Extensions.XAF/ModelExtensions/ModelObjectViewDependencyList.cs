using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using JetBrains.Annotations;

namespace Xpand.Extensions.XAF.ModelExtensions{
    
    [PublicAPI]
    public interface IModelObjectViewDependency:IModelNode{
        [Required][DataSourceProperty(nameof(ObjectViews))]
        IModelObjectView ObjectView{ get; set; }
        [Browsable(false)]
        IModelList<IModelObjectView> ObjectViews{ get; }
    }
    [DomainLogic(typeof(IModelObjectViewDependency))]
    public static class ModelObjectViewDependencyLogic {
        public static readonly ConcurrentDictionary<Type,Type> ObjectViewsMap=new ConcurrentDictionary<Type, Type>();

        public static void AddObjectViewMap(Type modelType,Type entityType ) 
            => ObjectViewsMap.TryAdd(modelType, entityType);

        public static IModelList<IModelObjectView> Get_ObjectViews(this IModelObjectViewDependency dependency){
            var key = ObjectViewsMap.Keys.First(type => type.IsInstanceOfType(dependency.Parent.Parent));
            return new CalculatedModelNodeList<IModelObjectView>(dependency.Application.Views.OfType<IModelObjectView>()
                .Where(view =>!view.ModelClass.TypeInfo.IsAbstract&& ObjectViewsMap[key].IsAssignableFrom(view.ModelClass.TypeInfo.Type)));
        }


    }
}