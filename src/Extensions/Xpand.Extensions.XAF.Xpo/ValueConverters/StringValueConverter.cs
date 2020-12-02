using System;
using DevExpress.Xpo.Metadata;

namespace Xpand.Extensions.XAF.Xpo.ValueConverters {

    public class StringValueConverter : ValueConverter {
        public override Type StorageType => typeof(string);

        public override object ConvertToStorageType(object value) => value?.ToString();

        public override object ConvertFromStorageType(object value) => value?.ToString();
    }
}