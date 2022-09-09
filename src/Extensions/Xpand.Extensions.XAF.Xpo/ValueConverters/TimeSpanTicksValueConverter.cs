using System;
using DevExpress.Xpo.Metadata;

namespace Xpand.Extensions.XAF.Xpo.ValueConverters{
    public class TimeSpanTicksValueConverter:ValueConverter {
        public override object ConvertToStorageType(object value) => ((TimeSpan?)value)?.Ticks ?? 0;

        public override object ConvertFromStorageType(object value) => TimeSpan.FromTicks(((long)(value??0L)));

        public override Type StorageType => typeof(long);
    }
}