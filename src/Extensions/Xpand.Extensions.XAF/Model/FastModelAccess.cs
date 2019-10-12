using System;
using System.Collections.Generic;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Extensions.XAF.Model{
    public partial class ModelExtensions{
        static readonly Lazy<FastModelEditorHelper> FastModelEditorHelperLazy=new Lazy<FastModelEditorHelper>(() => new FastModelEditorHelper());
        public static bool IsPropertyVisible(this IModelNode node,string propertyName){
            return FastModelEditorHelperLazy.Value.IsPropertyModelBrowsableVisible((ModelNode)node,propertyName);
        }
        public static T GetPropertyAttribute<T>(this IModelNode node,string propertyName) where T:Attribute{
            return FastModelEditorHelperLazy.Value.GetPropertyAttribute<T>((ModelNode)node,propertyName);
        }
        public static IList<T> GetPropertyAttributes<T>(this IModelNode node,string propertyName) where T:Attribute{
            return FastModelEditorHelperLazy.Value.GetPropertyAttributes<T>((ModelNode)node,propertyName);
        }
    }
}