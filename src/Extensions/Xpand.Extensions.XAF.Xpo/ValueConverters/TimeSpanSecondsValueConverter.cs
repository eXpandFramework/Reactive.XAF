using System;
using DevExpress.Xpo.Metadata;

namespace Xpand.Extensions.XAF.Xpo.ValueConverters{
    public class TimeSpanSecondsValueConverter:ValueConverter {
        public override object ConvertToStorageType(object value) 
            => ((TimeSpan?)value)?.TotalSeconds ?? 0;

        public override object ConvertFromStorageType(object value) => TimeSpan.FromSeconds(((long)(value ?? 0L)));

        public override Type StorageType => typeof(long);

    }
}