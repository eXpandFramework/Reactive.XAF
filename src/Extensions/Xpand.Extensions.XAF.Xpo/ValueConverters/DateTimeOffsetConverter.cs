using System;
using DevExpress.Xpo.Metadata;

namespace Xpand.Extensions.XAF.Xpo.ValueConverters{
    public class DateTimeOffsetConverter : ValueConverter {
        public override object ConvertFromStorageType(object value) => value;

        public override object ConvertToStorageType(object value) => value is DateTimeOffset dto ? dto.ToString() : value;

        public override Type StorageType => typeof(string);
    }
}