using System;
using DevExpress.Xpo.Metadata;

namespace Xpand.Extensions.XAF.Xpo.ValueConverters{
    public class TimeSpanTicksValueConverter:ValueConverter {
        public override object ConvertToStorageType(object value) => ((TimeSpan)value).Ticks;

        public override object ConvertFromStorageType(object value) => TimeSpan.FromTicks(((long)value));

        public override Type StorageType => typeof(long);
    }
}