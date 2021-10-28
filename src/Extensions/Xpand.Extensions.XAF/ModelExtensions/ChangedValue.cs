using System;
using DevExpress.ExpressApp.Model.Core;
using Xpand.Extensions.TypeExtensions;

namespace Xpand.Extensions.XAF.ModelExtensions {
    public static partial class ModelExtensions {
        public static object ChangedValue(this ModelValueInfo modelValueInfo, object value, Type destinationType) {
            var typeConverter = modelValueInfo.TypeConverter;
            return typeConverter != null ? typeConverter.ConvertFrom(value) : value.Change(destinationType);
        }
    }
}