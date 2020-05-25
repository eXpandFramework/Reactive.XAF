using System.Collections.Generic;
using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.Model{
    public static partial class ModelExtensions{
        public static CalculatedModelNodeList<T> ToCalculatedModelNodeList<T>(this IEnumerable<T> source) where T : IModelNode{
            return new CalculatedModelNodeList<T>(source);
        }
    }
}