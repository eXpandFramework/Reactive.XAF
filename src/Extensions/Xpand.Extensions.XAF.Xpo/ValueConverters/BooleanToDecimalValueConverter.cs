using System;
using DevExpress.Xpo.Metadata;

namespace Xpand.Extensions.XAF.Xpo.ValueConverters {
    
    public class BooleanToDecimalValueConverter : ValueConverter {
        public override Type StorageType => typeof(decimal);

        public override object ConvertToStorageType(object value) => value == null ? null : (object)Convert.ToDecimal(value);

        public override object ConvertFromStorageType(object value) => value == null ? null : (object)Convert.ToBoolean(value);
    }
}