using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Extensions.XAF.ModelExtensions{
    public static partial class ModelExtensions{
        public static void ReadFromModel(this IModelNode modelNode,IModelNode readFrom){
            var modelApplication = (ModelApplicationBase) readFrom.Application;
            for (var i = 0; i < modelApplication.AspectCount; i++){
                var aspect = modelApplication.GetAspect(i);
                var xml = new ModelXmlWriter().WriteToString(readFrom, i);
                
                if (!string.IsNullOrEmpty(xml)) {
                    new ModelXmlReader().ReadFromString(modelNode, aspect, xml);
                }
            }
        }

    }
}