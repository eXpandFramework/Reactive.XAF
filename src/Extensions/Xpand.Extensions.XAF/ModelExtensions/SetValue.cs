using System;
using DevExpress.ExpressApp.Model;
using Fasterflect;

namespace Xpand.Extensions.XAF.ModelExtensions {
    public static partial class ModelExtensions {
        public static void SetValue(this IModelNode modelNode, string propertyName, Type propertyType, object value) {
            if (propertyType == null) {
                var modelValueInfo = modelNode.GetModelValueInfo(propertyName).Item1;
                var changedValue = modelValueInfo.ChangedValue(value, modelValueInfo.PropertyType);
                modelNode.CallMethod(new[] { modelValueInfo.PropertyType }, "SetValue", propertyName, changedValue);
            }
            else
                modelNode.CallMethod(new[] { propertyType }, "SetValue", propertyName, value);
        }
    }
}