using System;
using DevExpress.Persistent.Base;
using DevExpress.Xpo.Metadata;

namespace Xpand.Extensions.XAF.Xpo.ValueConverters {
    public class TypeValueConverter : ValueConverter {
        public override Type StorageType => typeof (string);

        public override object ConvertFromStorageType(object value) {
            if (value == null)
                return null;

            try {
                return ReflectionHelper.GetType(value.ToString());
            }
            catch (TypeWasNotFoundException) {
            }

            return null;
        }

        public override object ConvertToStorageType(object value) => ((Type) value)?.FullName;
    }
}