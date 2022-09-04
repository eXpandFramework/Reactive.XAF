using System;
using DevExpress.Xpo.Metadata;
using Xpand.Extensions.Numeric;

namespace Xpand.Extensions.XAF.Xpo.ValueConverters{
    public class TimeSpanSecondsValueConverter:ValueConverter {
        public override object ConvertToStorageType(object value) => (long)((TimeSpan)value).TotalSeconds.Round();

        public override object ConvertFromStorageType(object value) => TimeSpan.FromSeconds(((long)value));

        public override Type StorageType => typeof(long);
    }
}