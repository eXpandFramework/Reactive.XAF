using System;
using DevExpress.Xpo.Metadata;

namespace Xpand.Extensions.XAF.Xpo.ValueConverters {
    public class NullValueConverter : ValueConverter {
        public override object ConvertToStorageType(object value) => null;

        public override object ConvertFromStorageType(object value) => null;

        public override Type StorageType => typeof(string);
    }
}