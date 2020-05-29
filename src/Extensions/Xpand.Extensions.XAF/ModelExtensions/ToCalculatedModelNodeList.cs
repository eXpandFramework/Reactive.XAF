using System.Collections.Generic;
using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.ModelExtensions{
    public static partial class ModelExtensions{
        public static CalculatedModelNodeList<T> ToCalculatedModelNodeList<T>(this IEnumerable<T> source) where T : IModelNode => new CalculatedModelNodeList<T>(source);
    }
}