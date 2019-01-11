using System;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Source.Extensions.XAF.Model{
    internal static partial class Extensions{
        public static void ReadFromModel(this IModelNode modelNode,IModelNode readFrom){
            modelNode.ReadFromModel(  readFrom, null);
        }

        public static void ReadFromModel(this IModelNode modelNode,
            IModelNode readFrom, Func<string, bool> aspectNamePredicate){
            var modelApplication = (ModelApplicationBase) readFrom.Application;
            for (var i = 0; i < modelApplication.AspectCount; i++){
                var aspect = modelApplication.GetAspect(i);
                var xml = new ModelXmlWriter().WriteToString(readFrom, i);
                if (!string.IsNullOrEmpty(xml))
                    new ModelXmlReader().ReadFromString(modelNode, aspect, xml);
            }
        }
    }
}