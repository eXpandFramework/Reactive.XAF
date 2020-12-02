using System;
using DevExpress.Xpo.Metadata;

namespace Xpand.Extensions.XAF.Xpo.ValueConverters{
    public class UriValueConverter : ValueConverter {
        public override Type StorageType => typeof(string);

        public override object ConvertFromStorageType(object value) => value == null ? null : new Uri(value.ToString());

        public override object ConvertToStorageType(object value) => value?.ToString();
    }
}