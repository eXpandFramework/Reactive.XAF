using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.Persistent.Base;
using JetBrains.Annotations;

namespace Xpand.Extensions.XAF.ModelExtensions{
    
    [KeyProperty(nameof(ObjectViewId))][PublicAPI]
    public interface IModelObjectViewDependency:IModelNode{
        [Browsable(false)]
        string ObjectViewId { get; set; }
        [Required][DataSourceProperty(nameof(ObjectViews))]
        IModelObjectView ObjectView{ get; set; }
        [Browsable(false)]
        IModelList<IModelObjectView> ObjectViews{ get; }
    }
    [DomainLogic(typeof(IModelObjectViewDependency))]
    public static class ModelObjectViewDependencyLogic {
        public static readonly Dictionary<System.Type,System.Type> ObjectViewsMap=new Dictionary<System.Type, System.Type>();
        public static IModelList<IModelObjectView> Get_ObjectViews(this IModelObjectViewDependency dependency){
            var key = ObjectViewsMap.Keys.First(type => type.IsInstanceOfType(dependency.Parent.Parent));
            return new CalculatedModelNodeList<IModelObjectView>(dependency.Application.Views.OfType<IModelObjectView>()
                .Where(view =>!view.ModelClass.TypeInfo.IsAbstract&& ObjectViewsMap[key].IsAssignableFrom(view.ModelClass.TypeInfo.Type)));
        }

        [UsedImplicitly]
        public static IModelObjectView Get_ObjectView(IModelObjectViewDependency todoObjectView) => 
            !string.IsNullOrEmpty(todoObjectView.ObjectViewId) ? todoObjectView.Application.Views[todoObjectView.ObjectViewId].AsObjectView : null;

        [UsedImplicitly]
        public static void Set_ObjectView(IModelObjectViewDependency todoObjectView, IModelObjectView value) => todoObjectView.ObjectViewId = value.Id;
    }
}