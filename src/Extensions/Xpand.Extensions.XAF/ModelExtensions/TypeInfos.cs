using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.ModelExtensions {
    public partial class ModelExtensions {
        public static IEnumerable<ITypeInfo> TypeInfos(this IModelBOModel boModel) 
            => boModel.Select(c => c.TypeInfo);
    }
}